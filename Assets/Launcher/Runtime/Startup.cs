using IniHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class Startup : MonoBehaviour
{
    [DllImport("wininet.dll")]
    private extern static bool InternetGetConnectedState(int Description, int ReservedValue);


    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessageTimeout(
    HandleRef hWnd,
    int msg,
    IntPtr wParam,
    IntPtr lParam,
    int flags,
    int timeout,
    out IntPtr pdwResult);

    const int SMTO_ABORTIFHUNG = 2;

    [DllImport("User32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("User32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(System.IntPtr hwnd, int nCmdShow);

    //设置窗口边框
    [DllImport("user32.dll")]
    public static extern IntPtr SetWindowLong(IntPtr hwnd, int _nIndex, int dwNewLong);

    //设置窗口位置，大小
    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    //是否是最小化窗口
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hWnd);


    //边框参数
    const uint SWP_SHOWWINDOW = 0x0040;
    const int GWL_STYLE = -16;
    const int WS_BORDER = 1;
    const int WS_POPUP = 0x800000;
    const int SW_SHOWMINIMIZED = 2;//(最小化窗口)


    [SerializeField]
    private Slider barSlider;

    [SerializeField]
    private Text txtTips;

    [SerializeField]
    private Image loadingCircle;

    [SerializeField]
    private Text loadingText;

    [SerializeField]
    private AudioSource source;


    /// <summary>
    /// 网络连接
    /// </summary>
    private bool netConnect = false;

    //检测间隔时间
    [SerializeField]
    private float spacingTime = 1f;
    private float detectTime = 0;
    private int stepIndex;
    private bool checking;
    private bool allInit;
    private bool animUpdate;
    private bool closeLauncher;

    [SerializeField]
    private float animSpeed = 20;

    private Dictionary<string, StartUp> dictAllStartUp = new Dictionary<string, StartUp>();

    private Dictionary<string, bool> dictAllWindowStyle = new Dictionary<string, bool>();

    private Dictionary<string, bool> dictAllCheck = new Dictionary<string, bool>();

    private Dictionary<string, bool> dictHideWindow = new Dictionary<string, bool>();

    private Dictionary<string, bool> dictMinimize = new Dictionary<string, bool>();

    public const string iniSection = "StartUp";

    public const string iniWindowStyle = "WindowStyle";

    public const string iniCheck = "CheckItem";

    public const string iniHideWindow = "HideWindow";

    public const string iniMinimize = "Minimize";

    private string tips;

    private string[] strTips =
    {
        "正在检测网络",
        "网络正常",
        "等待网络连接",
        "正在启动",
        "成功启动",
        "启动失败",
        "...",
        "系统启动完毕...",
        "网络未连接"
    };

    public class StartUp
    {
        public bool ison;
        public string path;
        public StartUp(bool ison, string path)
        {
            this.ison = ison;
            this.path = path;
        }
    }

    public void Init(string config)
    {
        detectTime = 0;
        stepIndex = 0;
        checking = false;
        tips = strTips[0];
        loadingCircle.fillAmount = 0;
        loadingText.text = "0";
        netConnect = false;
        allInit = true;
        animUpdate = false;
        //获取需要启动的配置文件
        dictAllStartUp.Clear();
        dictAllWindowStyle.Clear();
        dictAllCheck.Clear();
        dictHideWindow.Clear();
        dictMinimize.Clear();
        string iniPath = Application.streamingAssetsPath + "/Config.ini";

        if (!string.IsNullOrEmpty(config)&& config.EndsWith(".ini"))
        {
            iniPath = System.IO.Path.Combine(Application.streamingAssetsPath, config);
        }
        var allStartUp = IniFiles.INIGetAllItemKeys(iniPath, iniSection);
        for (int i = 0; i < allStartUp.Length; i++)
        {
            if (!dictAllStartUp.ContainsKey(allStartUp[i]))
            {
                var val = IniFiles.INIGetStringValue(iniPath, iniSection, allStartUp[i], null);
                if (val != null)
                {
                    if (!val.Contains('/'))
                    {
                        if(val.Contains(".exe"))
                        {
                            val = Path.Combine(System.IO.Directory.GetCurrentDirectory(), val.Replace(".exe", null), val);
                        }
                        else if(val.Contains(".bat"))
                        {
                            val = Path.Combine(System.IO.Directory.GetCurrentDirectory(), val.Replace(".bat", null), val);
                        }
                    }
                    dictAllStartUp.Add(allStartUp[i], new StartUp(false, val));
                }
            }
        }

        var allWindowStyle = IniFiles.INIGetAllItemKeys(iniPath, iniWindowStyle);
        for (int i = 0; i < allWindowStyle.Length; i++)
        {
            if (!dictAllWindowStyle.ContainsKey(allWindowStyle[i]))
            {
                var val = IniFiles.INIGetStringValue(iniPath, iniWindowStyle, allWindowStyle[i], null);
                if (val != null)
                {
                    dictAllWindowStyle.Add(allWindowStyle[i], bool.Parse(val));
                }
            }
        }

        var allCheckItem= IniFiles.INIGetAllItemKeys(iniPath, iniCheck);
        for (int i = 0; i < allCheckItem.Length; i++)
        {
            if (!dictAllCheck.ContainsKey(allCheckItem[i]))
            {
                var val = IniFiles.INIGetStringValue(iniPath, iniCheck, allCheckItem[i], null);
                if (val != null)
                {
                    dictAllCheck.Add(allCheckItem[i], bool.Parse(val));
                }
            }
        }

        var allHideWindow= IniFiles.INIGetAllItemKeys(iniPath, iniHideWindow);
        for (int i = 0; i < allHideWindow.Length; i++)
        {
            if (!dictHideWindow.ContainsKey(allHideWindow[i]))
            {
                var val = IniFiles.INIGetStringValue(iniPath, iniHideWindow, allHideWindow[i], null);
                if (val != null)
                {
                    dictHideWindow.Add(allHideWindow[i], bool.Parse(val));
                }
            }
        }

        var allMinimize= IniFiles.INIGetAllItemKeys(iniPath, iniMinimize);
        for (int i = 0; i < allMinimize.Length; i++)
        {
            if (!dictMinimize.ContainsKey(allMinimize[i]))
            {
                var val = IniFiles.INIGetStringValue(iniPath, iniMinimize, allMinimize[i], null);
                if (val != null)
                {
                    dictMinimize.Add(allMinimize[i], bool.Parse(val));
                }
            }
        }


        var files = dictAllStartUp.Values.ToArray();
        string[] paths = new string[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            paths[i] = files[i].path;
        }
        KillProcess(paths);
    }
    private void KillProcess(params string[] filePaths)
    {
        for (int i = 0; i < filePaths.Length; i++)
        {
            Process[] ps = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(filePaths[i]));
            if (ps.Length > 0)
            {
                foreach (Process p in ps)
                {
                    p.Kill();
                }
            }
        }
    }

    private void FixedUpdate()
    {
        HideWindows();
        MinimizeWindows();
        if (!closeLauncher)
        {
            IntPtr hwnd = FindWindow(null, "Launcher");
            if (hwnd == IntPtr.Zero)
            {
                return;
            }
            IntPtr activeWndHwnd = GetForegroundWindow();
            if (hwnd != activeWndHwnd)
            {
                ShowWindowAsync(hwnd, 3);
                SetForegroundWindow(hwnd);
            }
        }
        else
        {
            Process.GetCurrentProcess().Kill();
        }
    }

    private void HideWindows()
    {
        foreach (var item in dictHideWindow.Keys)
        {
            var ptr = FindWindow(null, item);
            if(ptr!=IntPtr.Zero)
            {
                ShowWindow(ptr, 0);
            }
        }
    }
    private float _time;
    private void MinimizeWindows()
    {
        _time += Time.deltaTime;
        if(_time>0.5f)
        {
            _time = 0;
            foreach (var item in dictMinimize.Keys)
            {
                var ptr = FindWindow(null, item);
                if (ptr != IntPtr.Zero)
                {
                    if (!IsIconic(ptr))
                    {
                        ShowWindow(ptr, 2);
                    }
                }
            }
        }
    }

    private void Update()
    {
        //if (!allInit)
        //{
        //    Init();
        //}
        if (!allInit) return;
        detectTime += Time.deltaTime;
        barSlider.value = detectTime;
        if (!checking)
        {
            if (detectTime >= spacingTime || animUpdate)
            {
                if (!animUpdate)
                    detectTime = 0;
                if (!netConnect)
                {
                    int Desc = 0;
                    netConnect = InternetGetConnectedState(Desc, 0);
                    if (netConnect)
                    {
                        source.Play();
                        tips = strTips[1];
                    }
                    else
                    {
                        if (dictAllCheck.ContainsKey("网络监测"))
                        {
                            if (!dictAllCheck["网络监测"])
                            {
                                tips = strTips[8];
                                netConnect = true;
                                return;
                            }
                        }
                            tips = strTips[2];
                    }
                }
                else
                {
                    var item = dictAllStartUp.ElementAt(stepIndex);
                    if (!item.Value.ison)
                    {
                        tips = strTips[3] + item.Key + strTips[6];
                        if (!RunExe(item.Value.path))
                        {
                            tips = strTips[5] + item.Key + strTips[6];
                        }
                        else
                        {
                            animUpdate = true;
                        }
                    }
                    else
                    {
                        //更新动画
                        if (loadingCircle.fillAmount < (float)((1f / dictAllStartUp.Count) * (stepIndex + 1)))
                        {
                            loadingCircle.fillAmount += Time.deltaTime * animSpeed;
                            loadingText.text = (loadingCircle.fillAmount * 100).ToString("0");
                        }
                        else
                        {
                            source.Play();
                            animUpdate = false;
                            if (stepIndex < dictAllStartUp.Count - 1)
                            {
                                stepIndex++;
                            }
                            else
                            {
                                checking = true;
                            }
                            tips = strTips[4] + item.Key + strTips[6];
                        }
                    }
                }
            }
            txtTips.text = tips;
        }
        else
        {
            if (detectTime >= spacingTime + 1)
            {
                barSlider.value = 1;
                loadingCircle.fillAmount = 1;
                loadingText.text = 100.ToString();
                txtTips.text = strTips[7];
                //GetResponding();
            }
            if (detectTime >= spacingTime + 2)
            {
                closeLauncher = true;
                //Application.Quit();
            }
        }
    }

    /// <summary>
    /// 获取程序是否未响应
    /// </summary>
    public bool RespondingWithinMs(Process process, int timeoutMs)
    {
        IntPtr ptr2;
        return SendMessageTimeout(
            new HandleRef(process, process.MainWindowHandle),
            0,
            IntPtr.Zero,
            IntPtr.Zero,
            SMTO_ABORTIFHUNG,
            timeoutMs,
            out ptr2) != IntPtr.Zero;
    }

    /// <summary>
    /// 获取启动程序状态
    /// </summary>
    private void GetResponding()
    {
        foreach (var item in dictAllStartUp)
        {
            Process[] ps = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(item.Value.path));
            if (ps.Length == 1)
            {
                foreach (Process p in ps)
                {
                    if (RespondingWithinMs(p, 1))
                    {
                        allInit = false;
                    }
                }
            }
            else
            {
                allInit = false;
            }
        }
    }

    /// <summary>
    /// 启动exe
    /// </summary>
    /// <param name="filePath">程序路径</param>
    public bool RunExe(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new Exception("filePath is empty");
        }
        if (!File.Exists(filePath))
        {
            throw new Exception(filePath + " is not exist");
        }
        string directory = Path.GetDirectoryName(filePath);
        Process[] ps = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(filePath));
        if (ps.Length > 0)
        {
            foreach (Process p in ps)
            {
                p.Kill();
            }
        }

        var item = dictAllStartUp.ElementAt(stepIndex);
        try
        {
            Process p = new Process();
            p.StartInfo.FileName = filePath;
            p.StartInfo.WorkingDirectory = directory;
            p.EnableRaisingEvents = true;
            //p.StartInfo.RedirectStandardError = false;
            if (dictAllWindowStyle.ContainsKey(item.Key))
            {
                if (!dictAllWindowStyle[item.Key])
                {
                    //p.StartInfo.CreateNoWindow = false;
                    //p.StartInfo.UseShellExecute = true;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    //ShowWindow(p.MainWindowHandle, SW_SHOWMINIMIZED);
                    //SetWindowLong(GetForegroundWindow(), GWL_STYLE, WS_POPUP);
                    //bool result = SetWindowPos(GetForegroundWindow(),0, 0,0,0,0, SWP_SHOWWINDOW);
                }
            }
            var ison = p.Start();
            dictAllStartUp[item.Key] = new StartUp(ison, item.Value.path);
            return ison;
        }
        catch (Exception ex)
        {
            return false;
            throw new Exception("系统错误：", ex);
        }
    }

    /// <summary>
    /// 程序退出
    /// </summary>
    private void p_Exited(object sender, EventArgs e)
    {
        allInit = false;
    }
}
