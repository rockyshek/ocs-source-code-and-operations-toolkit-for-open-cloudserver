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
Param( [string] $Expression="Out-Null", [string]$Transcript='', [switch] $IgnoreReturnCode)

Try
{
    $Invocation                   = (Get-Variable MyInvocation -Scope 0).Value
    $WcsScriptDirectory           =  Split-Path $Invocation.MyCommand.Path  
    $WCS_BASE_DIRECTORY           =  Split-Path $WcsScriptDirectory -Parent
    $WCS_BASE_DIRECTORY_NO_DRIVE  =  Split-Path $WCS_BASE_DIRECTORY -NoQualifier

    If ($WCS_BASE_DIRECTORY.Contains(' '))
    {
        Write-Host -ForegroundColor Red -NoNewline "These scripts do not support install directory with a space in the path`r"
    }

    #-------------------------------------------------------------------
    #  Include all other libraries
    #-------------------------------------------------------------------
    . "$WCS_BASE_DIRECTORY\Scripts\WcsScripts.ps1"
    #-------------------------------------------------------------------
    #  Setup transcript file
    #-------------------------------------------------------------------
    If ($Transcript -ne '')
    {
        try { Stop-Transcript -ErrorAction Stop | Out-Null } Catch {}

        try { Start-Transcript -Path $Transcript -Append -ErrorAction Stop | Out-Null } Catch {}
    }
    #-------------------------------------------------------------------
    # Run command
    #-------------------------------------------------------------------
    Write-Host "Invoke-Remote called with '$Expression'`r"

    If (-NOT $Expression.Contains('-ErrorAction')) 
    {
        $Results = (Invoke-Expression $Expression -ErrorAction Stop)
    }
    Else
    {
        $Results = (Invoke-Expression $Expression)
    }
    #-------------------------------------------------------------------
    # Return value 
    #-------------------------------------------------------------------
    If ($IgnoreReturnCode -or ($Results -eq $Null))
    {
        Return 0
    }
    ElseIf ($Results -is [int])
    {
        Return $Results
    }
    Else
    {
        Write-Host "Returned non-integer value: $Results"
        Return 5555
    }                
}
Catch
{
    #----------------------------------
    # Display the exception
    #----------------------------------
    If ($_.ErrorDetails -eq $null) 
    {   
        Try
        {
            $Position = $_.Exception.ErrorRecord.InvocationInfo.PositionMessage
        }
        Catch
        {
            $Position = $_.InvocationInfo.PositionMessage 
        }
        Write-Host -ForegroundColor Red  ("`r`nINVOKE-REMOTE EXCEPTION: {0}`r`n{1}`r`n`r" -f $_.Exception.Message,$Position)
    }
    Return $WCS_RETURN_CODE_UNKNOWN_ERROR 
}

