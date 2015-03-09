#=================================================================================================================================
# Copyright (c) Microsoft Corporation
# All rights reserved. 
# MIT License
#
# Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files 
# (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, 
# merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
# furnished to do so, subject to the following conditions:
# The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
# OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
# LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
# OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#=================================================================================================================================

Set-Alias -Name posttest  -Value Post-WcsTest
Set-Alias -Name pretest   -Value Pre-WcsTest
#-------------------------------------------------------------------------------------
# Post-WcsTest  
#-------------------------------------------------------------------------------------
Function Post-WcsTest
{
   <#
  .SYNOPSIS
   Logs configuration and error logs for post-test (ALIAS: PostTest)

  .DESCRIPTION
   Logs configuration and reports suspect errors in BMC SEL and Windows System
   Event Log.  Typically used for post test clean up and checking.
   
   The command reports the number of errors found by Check-WcsError  

   Results are stored in \<InstallDir>\Results\<ResultsDirectory>\Post-Test where
   <InstallDir> is typically \WcsTest and <ResultsDirectory> is the input parameter

   Configuration information stored in Get-WcsConfig_<datetime> directory

   BMC SEL and Windows Event Logs are stored in Check-WcsError_<datetime> directory

   .PARAMETER $ResultsDirectory
   Child directory to store the results in.   

   For example, if install directory is \wcstest and $ResultsDirectory is "TEC123"
   the log directory is \wcsTest\Results\TEC123\Post-Test
  
  .PARAMETER IncludeSelFile
   XML file that contains SEL entries to include as suspect errors

   See <InstallDir>\Scripts\References\DataFiles for example file

  .PARAMETER ExcludeSelFile
   XML file that contains SEL entries to exclude as suspect errors

   See <InstallDir>\Scripts\References\DataFiles for example file

  .PARAMETER IncludeEventFile
   XML file that contains Windows System Events to include as suspect errors

   See <InstallDir>\Scripts\References\DataFiles for example file

  .PARAMETER ExcludeEventFile
   XML file that contains Windows System Events to exclude as suspect errors

   See <InstallDir>\Scripts\References\DataFiles for example file

  .OUTPUTS
   Returns 0 on success, non-zero integer code on error

  .EXAMPLE
   Post-WcsTest TEC5000

   Logs configuration and error logs in \<InstallDir>\Results\TEC5000\Post-Test
   where <InstallDir> typically \WcsTest
          
  .COMPONENT
   WCS

  .FUNCTIONALITY
   Test  

   #>
    [CmdletBinding()] # Removed (PositionalBinding=$false) for Ver2.0 support
    Param
    ( 
        [Parameter(Mandatory=$true,Position=0)]                      [string]  $ResultsDirectory,
        [Parameter(Mandatory=$false)][ValidateNotNullOrEmpty()]      [string]  $IncludeSelFile    =  '',
        [Parameter(Mandatory=$false)][ValidateNotNullOrEmpty()]      [string]  $ExcludeSelFile    =  '',
        [Parameter(Mandatory=$false)][ValidateNotNullOrEmpty()]      [string]  $IncludeEventFile  =  '',
        [Parameter(Mandatory=$false)][ValidateNotNullOrEmpty()]      [string]  $ExcludeEventFile  =  ''
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation
        #-------------------------------------------------------
        # Check input parameters 
        #------------------------------------------------------- 
        If (($IncludeSelFile -ne '') -and (-not (Test-Path $IncludeSelFile)))
        {  
            Write-Host ("`r`n {0} aborted: Could not open IncludeSelFile '{1}'`r" -f $FunctionInfo.Name,$IncludeSelFile) -ForegroundColor Yellow
            Return $WCS_RETURN_CODE_FUNCTION_ABORTED
        }
        If (($ExcludeSelFile -ne '') -and (-not (Test-Path $ExcludeSelFile)))
        {  
            Write-Host ("`r`n {0} aborted: Could not open ExcludeSelFile '{1}'`r" -f $FunctionInfo.Name,$ExcludeSelFile) -ForegroundColor Yellow
            Return $WCS_RETURN_CODE_FUNCTION_ABORTED
        }
        If (($IncludeEventFile -ne '') -and (-not (Test-Path $IncludeEventFile)))
        {  
            Write-Host ("`r`n {0} aborted: Could not open IncludeEventFile '{1}'`r" -f $FunctionInfo.Name,$IncludeEventFile) -ForegroundColor Yellow
            Return $WCS_RETURN_CODE_FUNCTION_ABORTED
        }
        If (($ExcludeEventFile -ne '') -and (-not (Test-Path $ExcludeEventFile)))
        {  
            Write-Host ("`r`n {0} aborted: Could not open ExcludeEventFile '{1}'`r" -f $FunctionInfo.Name,$ExcludeEventFile) -ForegroundColor Yellow
            Return $WCS_RETURN_CODE_FUNCTION_ABORTED
        }
        #-------------------------------------------------------
        # Create the directory
        #-------------------------------------------------------
        $LogDirectory       = "$WCS_RESULTS_DIRECTORY\$ResultsDirectory\Post-Test"
        $ConfigDirectory    = ("$LogDirectory\Get-WcsConfig_{0}"-f (BaseLib_SimpleDate))
        $ErrorDir           = ("$LogDirectory\Check-WcsError_{0}" -f (BaseLib_SimpleDate)) 

        New-Item -Path $ConfigDirectory    -ItemType Container -ErrorAction SilentlyContinue | Out-Null
        New-Item -Path $LogDirectory       -ItemType Container -ErrorAction SilentlyContinue | Out-Null   
        #-------------------------------------------------------------------
        #  Display script header
        #-------------------------------------------------------------------
        Write-Host  "$WCS_HEADER_LINE`r"
        Write-Host  " Post-WcsTest:  This script will backup and check errors after a test`r"
        Write-Host  "$WCS_HEADER_LINE`r"
        Write-Host  " Log directory $LogDirectory `r`n`r"
        #-------------------------------------------------------------------
        #  Backup and check server errors
        #-------------------------------------------------------------------
        $ReturnCode   = Check-WcsError -LogDirectory $ErrorDir -IncludeEventFile $IncludeEventFile -ExcludeEventFile $ExcludeEventFile -IncludeSelFile $IncludeSelFile -ExcludeSelFile $ExcludeSelFile 
             
        $ReturnCode2  = Log-WcsConfig -Config (Get-WcsConfig) -File PostTestConfig -Path $ConfigDirectory  

        If (($ReturnCode -eq 0) -and ($ReturnCode2 -eq 0))
        {
            Write-Host  "`r`n Post-WcsTest Passed`r`n`r"
            Return $WCS_RETURN_CODE_SUCCESS       
        } 
        Else                                               
        { 
            Write-Host  "`r`n Post-WcsTest Failed`r`n`r" -ForegroundColor Yellow
            Return $ReturnCode +  $ReturnCode2
        }        
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
    Catch
    {
        $_.ErrorDetails  = CoreLib_FormatException $_  -FunctionInfo $FunctionInfo
        #----------------------------------------------
        # Take action (do nothing if SilentlyContinue)
        #---------------------------------------------- 
        If      ($ErrorActionPreference -eq 'Stop')             { Throw $_ }
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red -NoNewline $_.ErrorDetails }
    
        Return $WCS_RETURN_CODE_UNKNOWN_ERROR 
    }
 }
#-------------------------------------------------------------------------------------
# Pre-WcsTest 
#-------------------------------------------------------------------------------------
Function Pre-WcsTest()
{
   <#
  .SYNOPSIS
   Logs configuration and clears error logs (ALIAS: pretest)

  .DESCRIPTION
   Logs configuration and clears the BMC SEL and Windows Event Logs.
   Typically run to prepare for a test (pretest)

   Information from msinfo32 and the WCS configuration are logged.
   
   Results are stored in \<InstallDir>\Results\<ResultsDirectory>\Pre-Test where
   <InstallDir> is \WcsTest by default and <ResultsDirectory> is the input parameter

   Configuration information stored in Get-WcsConfig_<datetime> directory

   MsInfo32 information stored in the Log-msinfo32_<datetime> directory.

   .PARAMETER $ResultsDirectory
   Child directory to store the results in.   

   For example, if install directory is \wcstest and $ResultsDirectory is "TEC123"
   the log directory is \wcsTest\Results\TEC123\Pre-Test

  .OUTPUTS
   Returns 0 on success, non-zero integer code on error
    
  .EXAMPLE
   Pre-WcsTest TEC5000

   Logs configuration and error logs in \<InstallDir>\Results\TEC5000\Pre-Test
   where <InstallDir> typically \WcsTest
                
  .COMPONENT
   WCS

  .FUNCTIONALITY
   Test 

   #>

    [CmdletBinding()]
    Param
    (     
        [Parameter(Mandatory=$true)]  [string] $ResultsDirectory
    )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation
        #-------------------------------------------------------
        # Create the directory
        #-------------------------------------------------------
        $LogDirectory     = "$WCS_RESULTS_DIRECTORY\$ResultsDirectory\Pre-Test"
        $ConfigDirectory  =  ("$LogDirectory\Get-WcsConfig_{0}" -f (BaseLib_SimpleDate)) 

        New-Item -Path $ConfigDirectory  -ItemType Container -ErrorAction SilentlyContinue | Out-Null
        New-Item -Path $LogDirectory     -ItemType Container -ErrorAction SilentlyContinue | Out-Null    # OK if this one already exists
        #-------------------------------------------------------------------
        #  Display script header
        #-------------------------------------------------------------------
        Write-Host  "$WCS_HEADER_LINE`r"
        Write-Host  " Pre-WcsTest:  This function prepares the server for a test`r"
        Write-Host  "$WCS_HEADER_LINE`r"
        Write-Host  " Log directory $LogDirectory `r`n`r"
        #-------------------------------------------------------------------
        #  Backup and clear server errors
        #-------------------------------------------------------------------
        $ReturnCode = Clear-WcsError   

        #-------------------------------------------------------------------
        #  Use msinfo32 to log system configuration
        #-------------------------------------------------------------------
        If ( -Not (CoreLib_IsWinPE))
        {
            $ReturnCode2 = Log-MsInfo32   -LogDirectory  ("$LogDirectory\Log-Msinfo32_{0}" -f (BaseLib_SimpleDate))
        }
        Else
        {
            $ReturnCode2 = 0
        }
        #-------------------------------------------------------------------
        #  Log basic system info
        #-------------------------------------------------------------------
        $ReturnCode3 = Log-WcsConfig -Config (Get-WcsConfig) -File PreTestConfig -Path $ConfigDirectory  

        If (($ReturnCode -eq 0) -and ($ReturnCode2 -eq 0) -and ($ReturnCode3 -eq 0))
        {
            Write-Host  "`r`n Pre-WcsTest Passed`r`n`r"
            Return $WCS_RETURN_CODE_SUCCESS       
        } 
        Else                                               
        { 
            Write-Host  "`r`n Pre-WcsTest Failed`r`n`r" -ForegroundColor Yellow
            Return $WCS_RETURN_CODE_UNKNOWN_ERROR   
        }      
    }
    #------------------------------------------------------------
    # Default Catch block to handle all errors
    #------------------------------------------------------------
    Catch
    {
        $_.ErrorDetails  = CoreLib_FormatException $_  -FunctionInfo $FunctionInfo
        #----------------------------------------------
        # Take action (do nothing if SilentlyContinue)
        #---------------------------------------------- 
        If      ($ErrorActionPreference -eq 'Stop')             { Throw $_ }
        ElseIf  ($ErrorActionPreference -ne 'SilentlyContinue') { Write-Host -ForegroundColor Red -NoNewline $_.ErrorDetails }

        Return $WCS_RETURN_CODE_UNKNOWN_ERROR 
    }
}
