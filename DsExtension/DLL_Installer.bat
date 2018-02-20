echo off

mode con cols=150 lines=60

@setlocal enableextensions
@cd /d "%~dp0"

set Nom=%~1

setlocal enableDelayedExpansion
for %%i in (%Nom%*.dll) do (
set FichierDLL=%%~fi
set FichierTLB=%%~dpni.tlb
)

if exist "%FichierTLB%" (
call "DLL_Desinstaller.bat" %Nom%
)

set Titre=Inscription des DLLs
echo.
echo ================================================================
echo =
echo =                      %Titre%
echo =
echo ================================================================

Title %Titre%

echo.
echo Dossier courant :
echo    %cd%

set DossierCourant=%cd%
set DossierNET="%WINDIR%\Microsoft.NET\Framework\v4.0.30319"
set DossierNET64="%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"

If exist %DossierNET% (
cd /d %DossierNET%
)
If exist %DossierNET64% (
cd /d %DossierNET64%
)

echo.
echo Dossier du framework .NET
echo    %cd%

echo.
echo.
echo %Titre%
echo --------------------------------------------------------
echo Nom de la DLL a inscrire :
echo    %FichierDLL%
echo Nom de la TLB :
echo    %FichierTLB%

if "%DossierCourant%"==%cd% (
echo.
echo Le framework .NET v4.0.30319 n'est pas installe sur la machine
echo Il est necessaire au fonctionnement de la dll
echo.
echo FIN
exit
)

echo.
echo --------------------------------------------------------
RegAsm.exe "%FichierDLL%" /codebase /tlb:"%FichierTLB%"
echo --------------------------------------------------------
echo.
echo FIN

pause