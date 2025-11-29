; HearingAI Installer Script for NSIS
; Download NSIS from https://nsis.sourceforge.io/

!define PRODUCT_NAME "HearingAI"
!define PRODUCT_VERSION "1.0.0"
!define PRODUCT_PUBLISHER "Your Company Name"
!define PRODUCT_WEB_SITE "https://yourwebsite.com"
!define PRODUCT_DIR_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\HearingAI.exe"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"

; Modern interface settings
!include "MUI2.nsh"

; Interface configuration
!define MUI_ABORTWARNING
!define MUI_ICON "icon.ico"
!define MUI_UNICON "icon.ico"

; Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE.txt"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_INSTFILES

; Language
!insertmacro MUI_LANGUAGE "English"

; Installer details
Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "HearingAI_Setup.exe"
InstallDir "$PROGRAMFILES64\HearingAI"
InstallDirRegKey HKLM "${PRODUCT_DIR_REGKEY}" ""
ShowInstDetails show
ShowUnInstDetails show

Section "MainSection" SEC01
  SetOutPath "$INSTDIR"
  SetOverwrite ifnewer
  
  ; Copy files from standalone build
  File "dist\standalone\HearingAI.exe"
  File "dist\standalone\*.dll"
  File "README.md"
  
  ; Create shortcuts
  CreateDirectory "$SMPROGRAMS\HearingAI"
  CreateShortCut "$SMPROGRAMS\HearingAI\HearingAI.lnk" "$INSTDIR\HearingAI.exe"
  CreateShortCut "$DESKTOP\HearingAI.lnk" "$INSTDIR\HearingAI.exe"
  
  ; Registry entries
  WriteRegStr HKLM "${PRODUCT_DIR_REGKEY}" "" "$INSTDIR\HearingAI.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayName" "$(^Name)"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninst.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\HearingAI.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
  
  WriteUninstaller "$INSTDIR\uninst.exe"
SectionEnd

Section Uninstall
  Delete "$INSTDIR\uninst.exe"
  Delete "$INSTDIR\HearingAI.exe"
  Delete "$INSTDIR\*.dll"
  Delete "$INSTDIR\README.md"
  
  Delete "$SMPROGRAMS\HearingAI\HearingAI.lnk"
  Delete "$DESKTOP\HearingAI.lnk"
  RMDir "$SMPROGRAMS\HearingAI"
  RMDir "$INSTDIR"
  
  DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}"
  DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"
SectionEnd
