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
Try
{

    . \wcstest\scripts\wcsScripts.ps1

    #-----------------------------------------------------------------------------------------
    # Inject 12 corr and 12 uncorr ECC errors 
    #-----------------------------------------------------------------------------------------
    For ($Dimm=1;$Dimm -le 12; $Dimm++)
    {
        [byte[]]$RequestData = @(0,0,2, 0,0,0,0, 0,1,4,  0x0C,0x87,0x6F, 0xA0,0,$Dimm)
           
        $IpmiData = Invoke-WcsIpmi  0x44 $RequestData 0xA 

        [byte[]]$RequestData = @(0,0,2, 0,0,0,0, 0,1,4,  0x0C,0x87,0x6F, 0xA1,0,$Dimm)
           
        $IpmiData = Invoke-WcsIpmi  0x44 $RequestData 0xA 
    }
    Return 0
}
Catch
{
    Return 1
}
