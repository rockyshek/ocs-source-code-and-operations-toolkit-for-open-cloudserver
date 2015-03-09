@echo off
REM =================================================================================================================================
REM Copyright (c) Microsoft Corporation
REM All rights reserved. 
REM MIT License
REM
REM Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files 
REM (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, 
REM merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
REM furnished to do so, subject to the following conditions:
REM The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
REM
REM THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
REM OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
REM LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
REM OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
REM =================================================================================================================================
powershell -command ". \wcstest\scripts\WcsScripts.ps1 ; Cycle-WcsCheck -ExcludeEventFile C:\WcsTest\Scripts\References\ErrorFiles\ExcludePowerLossEvent.xml  -IncludeSel C:\WcsTest\Scripts\References\errorfiles\IncludeBootSel.xml"


REM The below line shows how to setup the bat file to run Iometer for 5 minutes on each cycle
REM -----------------------------------------------------------------------------------------
REM powershell -command ". \wcstest\scripts\WcsScripts.ps1 ; Cycle-WcsCheck -ExcludeEventFile C:\WcsTest\Scripts\References\ErrorFiles\ExcludePowerLossEvent.xml -Run 'Run-IOMeter -Time 5'"
