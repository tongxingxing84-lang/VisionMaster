using CommManagerModuleCs;
using DataQueueModuleCs;
using GlobalCameraModuleCs;
using GlobalVariableModuleCs;
using ImageSourceModuleCs;
using IMVSFastFeatureMatchModuCs;
using LightControlCs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VM.Core;
using VM.PlatformSDKCS;
using VMControls.Interface;

namespace VM二开
{
    public partial class Form1 : Form
    {
        private VmProcedure currentProcedure = null;
        private object currentModuParams = null;
        private object currentModule = null;
        private List<VmDynamicIODefine.IoNameInfo> inputInfos = new List<VmDynamicIODefine.IoNameInfo>();
        private List<VmDynamicIODefine.IoNameInfo> outputInfos = new List<VmDynamicIODefine.IoNameInfo>();

        //全局模块（不属于某个流程，方案里各只有一个时直接用第一个）
        private GlobalVariableModuleTool globalVariable = null;
        private DataQueueModuleTool dataQueue = null;
        private GlobalCameraModuleTool globalCamera = null;
        private CommManagerModuleTool commManager = null;

        //全局变量列表和相机列表，按钮里按选中项取数据
        private List<GlobalVarInfo> globalVarInfos = new List<GlobalVarInfo>();
        private CameraInfoList cameraInfoList = new CameraInfoList();

        public Form1()
        {
            //设置窗体大小
            this.Size = new Size(900, 450);
            InitializeComponent();

            //密码框默认不可用，勾选“使用密码”后才启用
            textBox2.Enabled = false;
            textBox2.UseSystemPasswordChar = true;
            textBox3.Enabled = false;
            textBox3.UseSystemPasswordChar = true;

            //通信地址默认 -1（一般不用传），设备 ID 默认 0
            domainUpDown1.Items.Clear();
            domainUpDown1.Items.Add("-1");
            for (int i = 0; i <= 99; i++)
                domainUpDown1.Items.Add(i.ToString());
            domainUpDown1.SelectedIndex = 0;

            domainUpDown2.Items.Clear();
            for (int i = 0; i <= 7; i++)
                domainUpDown2.Items.Add(i.ToString());
            domainUpDown2.SelectedIndex = 0;
        }

        //通信地址，读不到就按默认 -1
        private int GetAddressId()
        {
            int id;
            return int.TryParse(domainUpDown1.Text, out id) ? id : -1;
        }

        //通信设备 ID
        private int GetDeviceId()
        {
            int id;
            return int.TryParse(domainUpDown2.Text, out id) ? id : 0;
        }

        //读取方案密码：只有勾选“使用密码”才把文本框内容传给 SDK
        private string GetSolutionPassword()
        {
            return usePassword.Checked ? textBox2.Text : "";
        }

        //读取流程密码：只有勾选“使用密码”才生效
        private string GetProcedurePassword()
        {
            return checkBox3.Checked ? textBox3.Text : "";
        }

        //方案是否已经加载
        private bool IsSolutionLoaded()
        {
            return VmSolution.Instance != null &&
                   !string.IsNullOrWhiteSpace(VmSolution.Instance.SolutionPath);
        }

        //清空界面上的流程 / 模块 / 参数信息
        private void ClearSolutionView()
        {
            currentProcedure = null;
            currentModuParams = null;
            currentModule = null;

            processBox.Items.Clear();
            moduBox1.DataSource = null;
            moduBox2.DataSource = null;
            moduparaNameBox.DataSource = null;
            moduparaTxt.Text = "";
            comboBox15.DataSource = null;
            comboBox16.Text = "";
            comboBox18.DataSource = null;
            comboBox17.Text = "";
            textBox8.Text = "";

            //全局模块缓存和对应界面一起清空
            globalVariable = null;
            dataQueue = null;
            globalCamera = null;
            commManager = null;
            globalVarInfos.Clear();
            cameraInfoList = new CameraInfoList();
            comboBox3.Items.Clear();
            comboBox4.Items.Clear();
            comboBox5.Items.Clear();
            comboBox6.Items.Clear();
            comboBox7.Items.Clear();
            comboBox8.Items.Clear();
            comboBox9.Items.Clear();
            comboBox10.Items.Clear();
            comboBox11.Items.Clear();
            comboBox11.Text = "";
            comboBox12.Text = "";
            textBox5.Text = "";
            textBox6.Text = "";
        }

        //从方案里找第一个指定类型的全局模块
        private T GetFirstGlobalModule<T>() where T : VmModule
        {
            try
            {
                foreach (VmModule module in VmSolution.Instance.Modules)
                {
                    if (module is T) return (T)module;
                }
            }
            catch
            {
                //方案没加载时取不到，直接返回 null
            }
            return null;
        }

        //刷新全局模块区和下拉框内容
        private void RefreshGlobalModules()
        {
            try
            {
                globalVariable = GetFirstGlobalModule<GlobalVariableModuleTool>();
                dataQueue = GetFirstGlobalModule<DataQueueModuleTool>();
                globalCamera = GetFirstGlobalModule<GlobalCameraModuleTool>();
                commManager = GetFirstGlobalModule<CommManagerModuleTool>();

                //相机列表：取全局相机模块里配置的相机
                cameraInfoList = new CameraInfoList();
                comboBox3.Items.Clear();
                if (globalCamera != null)
                {
                    cameraInfoList = globalCamera.ModuParams.GetCameraInfoList();
                    for (int i = 0; i < cameraInfoList.nNum; i++)
                        comboBox3.Items.Add(cameraInfoList.astCameraInfo[i].strCameraName);

                    if (comboBox3.Items.Count > 0)
                        comboBox3.SelectedIndex = 0;
                }

                //控制器：光源设备类型
                comboBox4.Items.Clear();
                foreach (string name in Enum.GetNames(typeof(DeviceTypeEnum)))
                    comboBox4.Items.Add(name);
                if (comboBox4.Items.Count > 0)
                    comboBox4.SelectedIndex = 0;

                //光源列表：通道 1~4
                comboBox5.Items.Clear();
                for (int i = 1; i <= 4; i++)
                    comboBox5.Items.Add(i);
                if (comboBox5.Items.Count > 0)
                    comboBox5.SelectedIndex = 0;

                //数据队列：队列索引 0~9
                comboBox6.Items.Clear();
                for (int i = 0; i < 10; i++)
                    comboBox6.Items.Add(i);
                if (comboBox6.Items.Count > 0)
                    comboBox6.SelectedIndex = 0;

                //全局变量：变量名
                comboBox7.Items.Clear();
                globalVarInfos.Clear();
                if (globalVariable != null)
                {
                    List<GlobalVarInfo> list = globalVariable.GetAllGlobalVar();
                    if (list != null)
                        globalVarInfos = list;

                    foreach (GlobalVarInfo info in globalVarInfos)
                        comboBox7.Items.Add(info.strValueName);

                    if (comboBox7.Items.Count > 0)
                        comboBox7.SelectedIndex = 0;
                }

                //通信管理：设备 ID 0~7
                comboBox8.Items.Clear();
                for (int i = 0; i < 8; i++)
                    comboBox8.Items.Add(i);
                if (comboBox8.Items.Count > 0)
                    comboBox8.SelectedIndex = 0;

                //全局触发：列出方案里的全局触发模块名
                comboBox9.Items.Clear();
                foreach (VmModule module in VmSolution.Instance.Modules)
                {
                    //全局触发模块没有公开成员，这里按类型名判断
                    if (module.GetType().Name == "GlobalTriggerModuleTool")
                        comboBox9.Items.Add(module.Name);
                }
                if (comboBox9.Items.Count > 0)
                    comboBox9.SelectedIndex = 0;

                //全局脚本：SDK 没有脚本列表接口，这里只显示连续运行状态
                comboBox10.Items.Clear();
                comboBox10.Items.Add("连续运行：" +
                    (VmSolution.Instance.GloalScriptContinusRunEnable ? "开" : "关"));
                comboBox10.SelectedIndex = 0;

                comboBox11.Items.Clear();
                comboBox11.Text = "";
                comboBox12.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("刷新全局模块失败：" + ex.Message);
            }
        }

        // 定义一个用来装参数信息的辅助类（放在 Form1 类的内部或外部）
        public class ParamItem
        {
            public string Name { get; set; } // 参数名
            public object Value { get; set; } // 参数值
            public PropertyInfo Property { get; set; } // 保留属性信息，方便后续直接修改值

            // 重写 ToString 方法，决定在 ComboBox 里显示的样子
            public override string ToString()
            {
                return $"{Name}";
            }
        }

        //选择方案路径
        private void selectPathBtn_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    pathTxt.Text = openFileDialog.FileName; // 获取完整文件路径
                }
            }
        }

        //加载方案
        private void LoadprogrammeBtn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(pathTxt.Text))
            {
                MessageBox.Show("请先选择方案文件（.sol）！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                //已经加载过方案时先关闭，避免重复加载
                if (IsSolutionLoaded())
                    VmSolution.Instance.CloseSolution();

                //第二个参数是方案密码，第三个参数 true = 静默执行
                VmSolution.Load(pathTxt.Text, GetSolutionPassword(), silenceRun.Checked);

                MessageBox.Show("方案加载成功！");

                //获取流程信息
                processMessage();
            }
            catch (VmException ex)
            {
                MessageBox.Show($"加载方案失败，错误码 0x{ex.errorCode:X8}：{ex.errorMessage}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载方案失败：" + ex.Message);
            }
        }

        //获取流程信息
        private void processMessage()
        {
            try
            {
                //清空旧列表
                processBox.Items.Clear();

                //同步刷新全局模块区
                RefreshGlobalModules();
                //存储流程信息的列表
                List<VmProcedure> procedureList = new List<VmProcedure>();
                VmSolution.Instance.GetAllProcedureObjects(ref procedureList);

                if (string.IsNullOrWhiteSpace(VmSolution.Instance.SolutionPath))
                    return;
                //填充 ComboBox
                //for (int i = 0; i < procedureList.Count; i++)
                //{
                //    string processName = procedureList[i].Name;
                //    processBox.Items.Add(processName);
                //}

                foreach (var procedure in procedureList)
                {
                        processBox.Items.Add(procedure.Name);
                }

                if (processBox.Items.Count > 0)
                    processBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取流程列表失败：" + ex.Message);
            }
        }

        //获取模块信息
        private void moduMessage()
        {
            try
            {
                //清空旧列表
                moduBox1.DataSource = null;
                moduBox2.DataSource = null;
                if (currentProcedure == null) return;
                //获取模块列表
                ModuleInfoList stModuInfoList = currentProcedure.GetAllModuleList();

                //判断是否有模块，并提取模块名（通常用 strDisplayName）
                if (stModuInfoList.nNum > 0)
                {
                    // 提取所有非空的显示名称，并转为 List<string>
                    List<string> moduleNames = stModuInfoList.astModuleInfo
                        .Where(a => !string.IsNullOrEmpty(a.strDisplayName))
                        .Select(a => a.strDisplayName)
                        .ToList();

                    //绑定到 ComboBox
                    //先绑 moduBox2，再绑 moduBox1，避免左侧先选中时右侧还没数据
                    moduBox2.DataSource = new List<string>(moduleNames);
                    moduBox1.DataSource = moduleNames;
                    if (moduleNames.Count > 0)
                        moduBox1.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取流程列表失败：" + ex.Message);
            }
        }

        //流程选择事件
        private void processBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (processBox.SelectedItem == null) return;

            string processName = processBox.SelectedItem.ToString();

            try
            {
                // 通过名称索引器获取流程对象
                currentProcedure = (VmProcedure)VmSolution.Instance[processName];
                moduMessage();
                procedureParamMessage();

                //右侧 VM 主界面控件绑定当前流程
                try
                {
                    vmMainViewConfigControl1.BindSingleProcedure(processName);
                }
                catch
                {
                    //控件绑定失败不影响流程本身的操作
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取流程 [{processName}] 失败：{ex.Message}");
                currentProcedure = null;
            }
        }

        //模块选择事件
        private void moduBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            moduparaName();
        }

        //模块参数名称
        private void moduparaName()
        {
            if (currentProcedure == null || moduBox2.SelectedItem == null)
            {
                moduparaNameBox.DataSource = null;
                moduparaTxt.Text = "";
                return;
            }
            //获取当前选中流程的名称
            string currentProcessName = currentProcedure.Name;

            //获取模块列表中当前选中的模块名称
            string selectedModuleName = moduBox2.SelectedItem.ToString();

            //拼接完整的模块路径： "流程名.模块名"
            string moduleFullPath = $"{currentProcessName}.{selectedModuleName}";

            //通过索引器获取模块对象，并进行强类型转换
            var module = VmSolution.Instance[moduleFullPath];

            var moduParams = module.GetType().GetProperty("ModuParams")?.GetValue(module, null);
            currentModule = module;
            currentModuParams = moduParams;

            if (moduParams != null)
            {
                List<ParamItem> paramList = new List<ParamItem>();

                foreach (var prop in moduParams.GetType().GetProperties())
                {
                    if (prop.Name == "Tag" || !prop.CanRead) continue;

                    object value;
                    try
                    {
                        value = prop.GetValue(moduParams, null);   // 读不到就跳过
                    }
                    catch
                    {
                        continue;
                    }

                    paramList.Add(new ParamItem
                    {
                        Name = prop.Name,
                        Value = value,
                        Property = prop
                    });
                }

                // 绑定到 ComboBox
                moduparaNameBox.DataSource = paramList;
                moduparaTxt.Text = paramList.Count > 0
                    ? paramList[moduparaNameBox.SelectedIndex].Value?.ToString() ?? ""
                    : "";
            }
        }

        //保存方案
        private void saveprogrammeBtn_Click(object sender, EventArgs e)
        {
            if (!IsSolutionLoaded())
            {
                MessageBox.Show("当前没有加载任何方案！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                VmSolution.Save();
                MessageBox.Show("方案保存成功！");
            }
            catch (VmException ex)
            {
                MessageBox.Show($"保存方案失败，错误码 0x{ex.errorCode:X8}：{ex.errorMessage}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存方案失败：" + ex.Message);
            }
        }

        //方案另存为
        private void saveprogrammeOtherBtn_Click(object sender, EventArgs e)
        {
            if (!IsSolutionLoaded())
            {
                MessageBox.Show("当前没有加载任何方案！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                //设置对话框属性
                saveFileDialog.Title = "方案另存为";
                saveFileDialog.Filter = "VM方案文件 (*.sol)|*.sol|所有文件 (*.*)|*.*";
                saveFileDialog.FileName = System.IO.Path.GetFileName(VmSolution.Instance.SolutionPath);

                // 用户确认保存路径
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string savePath = saveFileDialog.FileName;

                    try
                    {
                        // 第二个参数是方案密码（没有就传空字符串）
                        VmSolution.SaveAs(savePath, GetSolutionPassword());

                        //另存后当前方案路径会变，界面同步刷新
                        pathTxt.Text = savePath;
                        MessageBox.Show("方案另存为成功！");
                    }
                    catch (VmException ex)
                    {
                        MessageBox.Show($"另存为失败，错误码 0x{ex.errorCode:X8}：{ex.errorMessage}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("另存为失败：" + ex.Message);
                    }
                }
            }
        }

        //执行方案
        private void RunprogrammeBtn_Click(object sender, EventArgs e)
        {
            if (!IsSolutionLoaded())
            {
                MessageBox.Show("当前没有加载任何方案，请先加载方案！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                VmSolution.Instance.SyncRun();
                MessageBox.Show("方案执行成功！");
            }
            catch (VmException ex)
            {
                MessageBox.Show($"方案执行失败，错误码 0x{ex.errorCode:X8}：{ex.errorMessage}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("方案执行失败：" + ex.Message);
            }
        }

        //方案状态
        private void ProjectStatusBtn_Click(object sender, EventArgs e)
        {
            if (!IsSolutionLoaded())
            {
                MessageBox.Show("当前没有加载任何方案！", "方案状态",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                List<VmProcedure> procedureList = new List<VmProcedure>();
                VmSolution.Instance.GetAllProcedureObjects(ref procedureList);

                string status =
                    $"方案路径：{VmSolution.Instance.SolutionPath}\r\n" +
                    $"是否运行中：{(VmSolution.Instance.IsRunning ? "是" : "否")}\r\n" +
                    $"是否带密码：{(VmSolution.Instance.IsHasPassword ? "是" : "否")}\r\n" +
                    $"流程数量：{procedureList.Count}\r\n" +
                    $"模块数量：{VmSolution.Instance.GetModuleCount()}\r\n" +
                    $"相机数量：{VmSolution.CameraNum}";

                MessageBox.Show(status, "方案状态",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取方案状态失败：" + ex.Message);
            }
        }

        //关闭方案
        private void closeprogramme_Click(object sender, EventArgs e)
        {
            if (!IsSolutionLoaded())
            {
                MessageBox.Show("当前没有加载任何方案！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                VmSolution.Instance.CloseSolution();
                ClearSolutionView();
                MessageBox.Show("方案关闭成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("关闭方案失败：" + ex.Message);
            }
        }

        //释放方案
        private void button8_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("释放后需要重新加载方案才能继续操作，确定释放吗？", "确认释放",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                VmSolution.Instance?.Dispose();
                ClearSolutionView();
                MessageBox.Show("方案释放成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("释放方案失败：" + ex.Message);
            }
        }

        //单次执行流程
        private void onceRunBtn_Click(object sender, EventArgs e)
        {
            if (currentProcedure == null)
            {
                MessageBox.Show("请先选择一个流程！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                //true：执行过程中响应界面（能刷新显示）
                currentProcedure.Run(true);

                if (currentProcedure.ModuResult.ErrorCode != 0)
                    MessageBox.Show("流程执行出错，错误码：" + currentProcedure.ModuResult.ErrorCode);
            }
            catch (VmException ex)
            {
                MessageBox.Show($"流程执行失败，错误码 0x{ex.errorCode:X8}：{ex.errorMessage}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("流程执行失败：" + ex.Message);
            }
        }

        //连续执行流程
        private void continuousRunBtn_Click(object sender, EventArgs e)
        {
            if (currentProcedure == null)
            {
                MessageBox.Show("请先选择一个流程！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            uint interval;
            if (!uint.TryParse(timeTxt.Text, out interval))
            {
                MessageBox.Show("请输入有效的执行时间间隔（整数，单位 ms）！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                //设置连续执行时间间隔
                currentProcedure.SetContinousRunInterval(interval);
                //流程开始连续执行
                currentProcedure.ContinuousRunEnable = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("开始连续执行失败：" + ex.Message);
            }
        }

        //停止连续执行流程
        private void stopBtn_Click(object sender, EventArgs e)
        {
            if (currentProcedure == null) return;
            currentProcedure.ContinuousRunEnable = false;
        }

        //启动该流程
        private void starprogrammeBtn_Click(object sender, EventArgs e)
        {
            if (currentProcedure == null)
            {
                MessageBox.Show("请先选择一个流程！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                VmSolution.Instance.EnableProcedure(currentProcedure.Name);
                MessageBox.Show($"流程 [{currentProcedure.Name}] 已启用！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("启用流程失败：" + ex.Message);
            }
        }

        //导出该流程
        private void outprogrammeBtn_Click(object sender, EventArgs e)
        {
            if (currentProcedure == null)
            {
                MessageBox.Show("请先选择一个流程！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Title = "导出流程";
                    //流程文件后缀是 .prc，不是 .vmpro
                    saveFileDialog.Filter = "VM流程文件 (*.prc)|*.prc|所有文件 (*.*)|*.*";
                    saveFileDialog.FileName = currentProcedure.Name;

                    // 用户确认保存路径
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string savePath = saveFileDialog.FileName;

                        //第二个参数是流程密码
                        currentProcedure.SaveAs(savePath, GetProcedurePassword());
                        MessageBox.Show($"流程 [{currentProcedure.Name}] 导出成功！", "提示",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出失败：" + ex.Message);
            }
        }

        //删除流程
        private void deleteprogrammeBtn_Click(object sender, EventArgs e)
        {
            if (currentProcedure == null)
            {
                MessageBox.Show("请先选择一个流程！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"确定要删除流程 [{currentProcedure.Name}] 吗？", "确认删除",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                //从方案里真正删掉，只 Dispose 不删方案里的节点
                VmSolution.Instance.DeleteOneProcedure(currentProcedure.Name);

                //重新拉一遍流程列表
                processMessage();

                if (processBox.Items.Count == 0)
                    MessageBox.Show("所有流程已清空。", "提示");
            }
            catch (Exception ex)
            {
                MessageBox.Show("删除流程失败：" + ex.Message);
            }
        }

        //导入流程
        private void intoprogrammeBtn_Click(object sender, EventArgs e)
        {
            if (!IsSolutionLoaded())
            {
                MessageBox.Show("请先加载一个方案，然后再导入流程！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //弹出打开文件对话框，选择要导入的流程文件
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "导入流程";
                openFileDialog.Filter = "VM流程文件 (*.prc)|*.prc|所有文件 (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        //第二个参数是流程密码，第三个参数 true = 同名全局变量覆盖
                        VmProcedure importedProcess = VmProcedure.Load(
                            openFileDialog.FileName, GetProcedurePassword(), true);

                        if (importedProcess != null)
                        {
                            //重新拉流程列表，再选中刚导入的流程
                            processMessage();
                            processBox.SelectedItem = importedProcess.Name;

                            MessageBox.Show($"流程 [{importedProcess.Name}] 导入成功！", "成功",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("导入流程失败：" + ex.Message, "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        //模块参数名称切换事件
        private void moduparaNameBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (moduparaNameBox.SelectedItem is ParamItem item)
                moduparaTxt.Text = item.Value?.ToString() ?? "";
        }

        //修改模块参数值
        private void modifyBtn2_Click(object sender, EventArgs e)
        {
            if (currentModuParams == null || moduparaNameBox.SelectedItem == null) return;

            try
            {
                ParamItem p = (ParamItem)moduparaNameBox.SelectedItem;
                object newValue = Convert.ChangeType(moduparaTxt.Text, p.Property.PropertyType);

                // 写进参数对象
                p.Property.SetValue(currentModuParams, newValue);

                // 同步缓存，切换参数名回来时才能看到新值
                p.Value = newValue;

                MessageBox.Show($"写入值：{newValue}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("修改失败：" + ex.Message);
            }

        }

        //获取流程输入/输出参数列表
        private void procedureParamMessage()
        {
            try
            {
                inputInfos = currentProcedure.ModuParams.GetAllInputNameInfo() ?? new List<VmDynamicIODefine.IoNameInfo>();
                outputInfos = currentProcedure.ModuResult.GetAllOutputNameInfo() ?? new List<VmDynamicIODefine.IoNameInfo>();

                comboBox15.DataSource = inputInfos.Select(a => a.Name).ToList();
                comboBox18.DataSource = outputInfos.Select(a => a.Name).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取流程参数失败：" + ex.Message);
            }
        }

        private void comboBox15_SelectedIndexChanged(object sender, EventArgs e)
        {
            var info = inputInfos.FirstOrDefault(a => a.Name == comboBox15.Text);
            comboBox16.Text = string.IsNullOrEmpty(info.Name) ? "" : info.TypeName.ToString();
        }

        private void comboBox18_SelectedIndexChanged(object sender, EventArgs e)
        {
            var info = outputInfos.FirstOrDefault(a => a.Name == comboBox18.Text);
            comboBox17.Text = string.IsNullOrEmpty(info.Name) ? "" : info.TypeName.ToString();
        }

        //读取流程输入参数值
        private void button25_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentProcedure == null || string.IsNullOrEmpty(comboBox15.Text)) return;

                VmIO io = currentProcedure.Inputs.GetIO(comboBox15.Text);
                textBox8.Text = io?.GetValue(0, 1)?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取失败：" + ex.Message);
            }
        }

        private void button24_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentProcedure == null || string.IsNullOrEmpty(comboBox15.Text)) return;

                string name = comboBox15.Text;
                string text = textBox8.Text;

                switch (comboBox16.Text)
                {
                    case "IMVS_GRAP_TYPE_INT":
                        currentProcedure.ModuParams.SetInputInt(name, new[] { int.Parse(text) });
                        break;
                    case "IMVS_GRAP_TYPE_FLOAT":
                        currentProcedure.ModuParams.SetInputFloat(name, new[] { float.Parse(text) });
                        break;
                    case "IMVS_GRAP_TYPE_STRING":
                        currentProcedure.ModuParams.SetInputString(name,
                            new[] { new InputStringData { strValue = text } });
                        break;
                    default:
                        MessageBox.Show("暂不支持该类型：" + comboBox16.Text);
                        return;
                }

                MessageBox.Show("写入成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入失败：" + ex.Message);
            }
        }

        //勾选“使用密码”时启用方案密码框
        private void usePassword_CheckedChanged(object sender, EventArgs e)
        {
            textBox2.Enabled = usePassword.Checked;
            if (!usePassword.Checked)
                textBox2.Text = "";
        }

        //勾选“使用密码”时启用流程密码框
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            textBox3.Enabled = checkBox3.Checked;
            if (!checkBox3.Checked)
                textBox3.Text = "";
        }

        //模块列表（左侧只用来浏览）与右侧选择框保持同步
        private void moduBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = moduBox1.SelectedIndex;

            //moduBox2 还没绑定数据时，索引可能越界，先判断
            if (index >= 0 && index < moduBox2.Items.Count)
                moduBox2.SelectedIndex = index;
        }

        //读取流程输出结果
        private void button21_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentProcedure == null || string.IsNullOrEmpty(comboBox18.Text))
                    return;

                string value = ReadOutputValue(comboBox18.Text);
                MessageBox.Show($"{comboBox18.Text} = {value}", "流程结果值",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取结果失败：" + ex.Message);
            }
        }

        //按输出类型读取流程输出值
        private string ReadOutputValue(string name)
        {
            ProcedureResult r = currentProcedure.ModuResult;

            var info = outputInfos.FirstOrDefault(a => a.Name == name);
            if (string.IsNullOrEmpty(info.Name)) return "";

            switch (info.TypeName.ToString())
            {
                case "IMVS_GRAP_TYPE_INT":
                    {
                        IntDataArray v = r.GetOutputInt(name);
                        return v.nValueNum < 1 || v.pIntVal == null
                            ? ""
                            : string.Join(",", v.pIntVal);
                    }

                case "IMVS_GRAP_TYPE_FLOAT":
                    {
                        FloatDataArray v = r.GetOutputFloat(name);
                        return v.nValueNum < 1 || v.pFloatVal == null
                            ? ""
                            : string.Join(",", v.pFloatVal);
                    }

                case "IMVS_GRAP_TYPE_STRING":
                    {
                        StringDataArray v = r.GetOutputString(name);
                        return v.nValueNum < 1 || v.astStringVal == null
                            ? ""
                            : v.astStringVal[0].strValue;
                    }

                case "IMVS_BASE_TYPE_IMAGE_DATA":
                    {
                        ImageBaseData img = r.GetOutputImageV2(name);
                        if (img == null) return "";
                        return $"图像 {img.Width}×{img.Height}";
                    }

                default:
                    return "";
            }
        }

        //相机信息
        private void button16_Click(object sender, EventArgs e)
        {
            if (globalCamera == null)
            {
                MessageBox.Show("方案里没有全局相机模块！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (comboBox3.SelectedIndex < 0 || comboBox3.SelectedIndex >= cameraInfoList.nNum)
            {
                MessageBox.Show("请先选择一台相机！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                CameraInformation info = cameraInfoList.astCameraInfo[comboBox3.SelectedIndex];
                string status =
                    $"相机名称：{info.strCameraName}\r\n" +
                    $"相机序列号：{info.strCameraSN}\r\n" +
                    $"连接状态：{(globalCamera.bIsCameraConnect() ? "已连接" : "未连接")}";

                MessageBox.Show(status, "相机信息",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取相机信息失败：" + ex.Message);
            }
        }

        //读取数据队列
        private void button17_Click(object sender, EventArgs e)
        {
            if (dataQueue == null)
            {
                MessageBox.Show("方案里没有数据队列模块！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int index;
            if (!int.TryParse(comboBox6.Text, out index))
            {
                MessageBox.Show("请先选择队列索引！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                StringValue value = dataQueue.GetStringData(index);
                if (value.nNum < 1 || value.astValue == null)
                {
                    textBox5.Text = "";
                    return;
                }

                List<string> list = new List<string>();
                for (int i = 0; i < value.nNum; i++)
                    list.Add(value.astValue[i].strValue);

                textBox5.Text = string.Join(",", list);
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取数据队列失败：" + ex.Message);
            }
        }

        //读取全局变量
        private void button18_Click(object sender, EventArgs e)
        {
            if (globalVariable == null)
            {
                MessageBox.Show("方案里没有全局变量模块！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (comboBox7.SelectedIndex < 0)
            {
                MessageBox.Show("请先选择一个全局变量！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                textBox6.Text = globalVariable.GetGlobalVar(comboBox7.Text) ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取全局变量失败：" + ex.Message);
            }
        }

        //写入全局变量
        private void button19_Click(object sender, EventArgs e)
        {
            if (globalVariable == null)
            {
                MessageBox.Show("方案里没有全局变量模块！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (comboBox7.SelectedIndex < 0)
            {
                MessageBox.Show("请先选择一个全局变量！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                globalVariable.SetGlobalVar(comboBox7.Text, textBox6.Text);
                MessageBox.Show($"全局变量 [{comboBox7.Text}] 写入成功！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入全局变量失败：" + ex.Message);
            }
        }

        //获取通信设备状态
        private void button20_Click(object sender, EventArgs e)
        {
            if (commManager == null)
            {
                MessageBox.Show("方案里没有通信管理模块！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int deviceId = GetDeviceId();
                bool online = commManager.bIsDeviceConnect(deviceId);

                MessageBox.Show($"设备 ID {deviceId}：{(online ? "已连接" : "未连接")}", "设备状态",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取设备状态失败：" + ex.Message);
            }
        }

        //切换全局变量时直接显示当前值
        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (globalVariable == null || comboBox7.SelectedIndex < 0) return;

            try
            {
                textBox6.Text = globalVariable.GetGlobalVar(comboBox7.Text) ?? "";
            }
            catch
            {
                //读取失败时保留原内容
            }
        }

        //切换通信设备时同步设备ID，并读取接收数据
        private void comboBox8_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox8.SelectedIndex >= 0 && comboBox8.SelectedIndex < domainUpDown2.Items.Count)
                domainUpDown2.SelectedIndex = comboBox8.SelectedIndex;

            ReadCommData();
        }

        //通信_发送框回车发送
        private void comboBox12_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;

            e.SuppressKeyPress = true;
            SendCommData();
        }

        //读取通信模块的接收数据，显示在“通信_接收”里
        private void ReadCommData()
        {
            if (commManager == null || comboBox8.SelectedIndex < 0) return;

            try
            {
                int deviceId = GetDeviceId();
                int addressId = GetAddressId();

                byte[] data = new byte[1024];
                commManager.GetReadData(deviceId, ref data, data.Length, addressId);

                comboBox11.Text = data == null
                    ? ""
                    : Encoding.UTF8.GetString(data).TrimEnd('\0');
            }
            catch
            {
                //读取失败时保持原内容
            }
        }

        //发送“通信_发送”里的内容
        private void SendCommData()
        {
            if (commManager == null)
            {
                MessageBox.Show("方案里没有通信管理模块！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (comboBox8.SelectedIndex < 0)
            {
                MessageBox.Show("请先选择通信设备！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int deviceId = GetDeviceId();
                int addressId = GetAddressId();

                commManager.SetString(deviceId, comboBox12.Text, addressId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("发送失败：" + ex.Message);
            }
        }
    }
}
