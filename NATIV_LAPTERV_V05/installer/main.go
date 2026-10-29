//go:build windows
package main
import (
    "embed"
    "fmt"
    "os"
    "os/exec"
    "path/filepath"
    "strings"
    "syscall"
    "unsafe"
)
//go:embed assets/Lapterv.exe assets/Lapterv_Eltavolitas.exe
var files embed.FS
const ver="0.5.0"
func message(s string,style uintptr) uintptr {
  title,_:=syscall.UTF16PtrFromString("Lapterv " + ver + " – natív Windows telepítő")
  body,_:=syscall.UTF16PtrFromString(s)
  n,_,_:=syscall.NewLazyDLL("user32.dll").NewProc("MessageBoxW").Call(0,uintptr(unsafe.Pointer(body)),uintptr(unsafe.Pointer(title)),style)
  return n
}
func psq(s string)string{return "'"+strings.ReplaceAll(s,"'","''")+"'"}
func copyFile(root,filename string)error{
  b,e:=files.ReadFile("assets/"+filename);if e!=nil{return e}
  if err:=os.MkdirAll(root,0700);err!=nil{return err}
  p:=filepath.Join(root,filename)
  tmp:=p+".new";if err:=os.WriteFile(tmp,b,0600);err!=nil{return err}
  if err:=os.Rename(tmp,p);err!=nil{_ = os.Remove(p);if err2:=os.Rename(tmp,p);err2!=nil{return err2}}
  return nil
}
func shortcut(exe string)error {
  scr:=`$s=New-Object -ComObject WScript.Shell; $exe=`+psq(exe)+`; `+
  `foreach($d in @([Environment]::GetFolderPath('DesktopDirectory'),[Environment]::GetFolderPath('Programs'))) `+
  `{if($d){$lnk=$s.CreateShortcut((Join-Path $d 'Lapterv.lnk'));$lnk.TargetPath=$exe;`+
  `$lnk.WorkingDirectory=(Split-Path -Parent $exe);$lnk.Description='Lapterv - nativ szabasztervezo';$lnk.Save()}}`
  c:=exec.Command("powershell.exe","-NoProfile","-NonInteractive","-Command",scr)
  c.SysProcAttr=&syscall.SysProcAttr{HideWindow:true}
  out,e:=c.CombinedOutput();if e!=nil{return fmt.Errorf("%w: %s",e,string(out))};return nil
}
func registry(name,value string)error {
  c:=exec.Command("reg.exe","ADD",`HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\Lapterv`,
    "/v",name,"/t","REG_SZ","/d",value,"/f")
  c.SysProcAttr=&syscall.SysProcAttr{HideWindow:true};return c.Run()
}
func main(){
  root:=filepath.Join(os.Getenv("LOCALAPPDATA"),"Programs","Lapterv")
  if os.Getenv("LOCALAPPDATA")==""{message("Nem talalom a LOCALAPPDATA mappat.",0x10);return}
  if message("Telepited a Lapterv 0.5 NATÍV Windows próbaverzióját?\n\n"+
    "NEM böngészőprogram: Windows Forms alkalmazás, Windows 10/11 x64.\n"+
    "A kísérleti .xPrg fájl még NEM hitelesített gépi utasítás.\n\nTelepítés: "+root,0x24)!=6{return}
  for _,file:=range []string{"Lapterv.exe","Lapterv_Eltavolitas.exe"}{
    if err:=copyFile(root,file);err!=nil{message("Telepitesi hiba (előbb zárd be a Laptervet):\n"+err.Error(),0x10);return}
  }
  target:=filepath.Join(root,"Lapterv.exe")
  if e:=shortcut(target);e!=nil{message("A telepítés megtörtént, de a parancsikon nem készült el: "+e.Error(),0x30)}
  for _,kv:=range [][2]string{{"DisplayName","Lapterv – natív próbaverzió"},{"DisplayVersion",ver},
    {"Publisher","Lapterv"},{"InstallLocation",root},{"DisplayIcon",target},
    {"UninstallString",`"`+filepath.Join(root,"Lapterv_Eltavolitas.exe")+`"`}}{
    _=registry(kv[0],kv[1])
  }
  if message("A natív Lapterv telepítése kész.\nElindítod?",0x24)==6{
    if e:=exec.Command(target).Start();e!=nil{message(e.Error(),0x10)}
  }
}
