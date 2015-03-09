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

 
#-------------------------------------------------------------------------------------
# Flush-WcsRaidCache  
#-------------------------------------------------------------------------------------
Function Flush-WcsRaidCache
{
   <#
  .SYNOPSIS
   Flushes the RAID adapter's cache

  .DESCRIPTION
   Flushes the RAID cache using the RAID adapter utility.

   This may result in loss of data if the cache is dirty and a logical 
   disk is missing or failed.  Use with caution.

   Only supports the LSI 9270 RAID adapter

  .OUTPUTS
   Returns 0 on success, non-zero integer code on error

  .EXAMPLE
   Flush-WcsRaidCache

   Flushes the RAID cache.
          
  .COMPONENT
   WCS

  .FUNCTIONALITY
   RAID    

   #>
    [CmdletBinding()]
    Param( )

    Try
    {
        #-------------------------------------------------------
        # Get calling details for debug
        #-------------------------------------------------------
        $FunctionInfo = CoreLib_FormatFunctionInfo $MyInvocation

        Write-Host (" Flushing the RAID cache with {0}`r`n`r" -f $FunctionInfo.Name)

        #-------------------------------------------------------
        # For now the only supported RAID is the LSI 9270
        #-------------------------------------------------------
        Return (Flush-LsiRaidCache -ErrorAction Stop)
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
