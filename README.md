## OCS Chassis Manager 

Open cloud server Chassis Manager is a management software for rack level devices like server, fan and PSU. 
It primarily consists of two modules -- Chassis Manager Service and WcsCli. Chassis Manager Service provides implementation to manage various sub-services like fan service, PSU service, power control service, etc. The WcsCli provides a framework to carry out system management operations. This framework is exposed in two forms -- RESTful APIs for automated management; and a command-line interface for manual management.

The intent of this community project is to collaborate with Open Compute Project Foundation (OCP) to build a thriving ecosystem of OSS within OCP and contribute this project to OCP.

If your intent is to use the Chassis Manager software without contributing back to this project, then use the MASTER branch which holds the approved and stable public releases.

If your goal is to improve or extend the code and contribute back to this project, then you should make your changes in, and submit a pull request against, the DEVELOPMENT branch. Read through our wiki section on [how to contribute] (https://github.com/opencomputeproject/ocs-source-code-and-operations-toolkit-for-open-cloudserver/wiki/How-To-Contribute) for a walk-through of the contribution process.

All new work should be in the development sub branches. Master is now reserved to tag builds.


## OCS Deployment Operations Toolkit
The Open CloudServer (OCS) Deployment Toolkit is a collection of scripts and utilities for updating, diagnosing, and testing OCS servers and chassis managers.  This Toolkit provides a one stop shop for utilities, tests and diagnostics that provide: 

- Diagnostics 

Identify defective components such as HDD, DIMM, and processor 

View, log, and compare configurations  

Read, clear and log errors 

- Stressors 

System stress tests to identify intermittent problems 

Component specific stress tests  

Cycling tests to identify intermittent initialization problems 

-  Updates 

Update programmable components such as BIOS and BMC 

Batch update of all programmable components   

- Miscellaneous 

Debug functions to execute IPMI and REST commands  

The Toolkit runs on 64 bit versions of WinPE version 5.1 or later, Windows Server 2012 or later, and Windows 8.1 or later. The Toolkit can be deployed on bootable WinPE USB flash drives, WinPE RAM drives (from PXE Server), and drives with the Windows Server and Desktop Operating Systems.

The intent of this community project is to collaborate with Open Compute Project Foundation (OCP) to build a thriving ecosystem of OSS within OCP and contribute this project to OCP. 

If your intent is to use the Operations Toolkit software without contributing back to this project, then use the MASTER branch which holds the approved and stable public releases. 

If your goal is to improve or extend the code and contribute back to this project, then you should make your changes in, and submit a pull request against, the DEVELOPMENT branch. Read through our wiki section on [how to contribute] (https://github.com/opencomputeproject/ocs-source-code-and-operations-toolkit-for-open-cloudserver/wiki/How-To-Contribute) for a walk-through of the contribution process.

All new work should be in the development branch. Master is now reserved to tag builds 



## Quick Start Chassis Manager

- Clone the repo: git clone https://github.com/opencomputeproject/ocs-source-code-and-operations-toolkit-for-open-cloudserver.git

- Download the zip version of the repo (see the right-side pane)

- Microsoft Visual Studio build environment. README contains further instructions on how to build the project and generate required executables. 

## Quick Start OCS deployment toolkit

- Clone the repo: git clone 
https://github.com/opencomputeproject/ocs-source-code-and-operations-toolkit-for-open-cloudserver.git

- Download the zip version of the repo (see the right-side pane)

Additional helpful information can be found in OCSToolsUserGuide.PDF



## Components Included Chassis Manager(version 2.5)

(i) Chassis Manager -- This folder contains all source/related files for the Chassis Manager Service. The Chassis Manager Service includes 6 main services related to managing fan, PSU, power control, blade management, Top-of-rack (TOR), security and chassis manager control. 

(ii) Contracts -- This folder contains all related files for Windows Chassis Manager Service contract.

(iii) IPMI -- This folder contains all source/related files for the implementation of native Windows intelligent platform management interface (IPMI) driver. This is required to provide the capability of in-band management of servers through the operating system. 

(iv) WcsCli -- This folder contains all source/related files for the framework that the Chassis Manager (CM) leverages to manage the rack level devices. Through this module, a CM provides the front end through the application interface (RESTful web API) for automated management and the command-line interface for manual management. It implements various commands required to manage all devices within the rack and to establish communication directly with the blade management system through a serial multiplexor.

(v) CM_TestAutomation -- This folder contains all source/related files for testing, chassis manager funcation validation test which can be used to validate Chassis Manager using the IPMI protocol.

(vi) ReportGenerator -- This folder contains all source/related files for summary report generation for CM_TestAutomation, which can be used to generate summary of test report.

(vii) Chassis Validation -- This folder contains all source/related files for testing, chassis manager functional validation test which can be used to validate Chassis Manager using the XML over HTTP RESTful api.

## Components OCS deployment toolkit

-Collection of scripts and utilities for updating, diagnosing, and testing OCS servers and chassis managers. 


## Prerequisites Chassis Manager

- .Net Framework 4.0 Full version

- .Net Framework 2.0 Software Development Kit (SDK)

- Visual Studio for building and testing solution

- Windows machine: Windows Server operating system

- Note that no other external dependencies (DLLs or EXEs) are required to be installed as all are self-contained in respective project directory. 

## Prerequisites Deployment Toolkit

PowerShell ExecutionPolicy must allow script execution 

- The Toolkit requires the PowerShell execution policy be set to allow the running of scripts.  

- One possible way to enable script execution is to run this command: PowerShell -Command Set-ExecutionPolicy RemoteSigned -Force 

Run As Administrator 

- Many of the commands must be run as administrator because they read low level hardware information.  
	
- If commands are not run as an administrator they may return incomplete or incorrect information. 

- Note that starting the Toolkit using the desktop shortcut automatically runs the Toolkit as administrator.    



## BUILD and Install Instructions

MCS-ChassisManager is developed in Microsoft Visual Studio environment and is completely written in C#. To build the serivce (ChassisManager) or command management interface (WcsCli), please follow the below steps:

- Import the project in Visual Studio by browsing and importing the specific project solution file. We have tested this on both Visual Studio 2012 Ultimate and Visual Studio Express versions.

- Build the project in Visual Studio by going to menu->BUILD->Build Solution or Ctrl+Shift+B.

- After successful build, the project executable is created under a newly created sub-directory called 'bin' (under the parent project directory). 


To install Chassis Manager Service, use the following commands:

Start service: net start chassismanager

Stop service: net stop chassismanager

## Test Instructions Chassis Manager

cmStress can be used to validate the chassis through Ipmi protocol directly.

To run the test, first build the ChassisManagerTestAutomationUserInterface solution following the BUILD steps as above.

Open a command prompt window and run the test application under the 'bin' folder.

Here are some examples:

To get more detailed help on the test tool, please run:

cmStress.exe /Help

## Test Instructions OCS ToolKit


Detail helpful information can be found in OCSToolsUserGuide.PDF

