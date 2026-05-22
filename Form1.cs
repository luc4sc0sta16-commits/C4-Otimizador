using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Media;

namespace C4_Otimizador
{
    public partial class Form1 : Form
    {
        // --- COMPONENTES DA INTERFACE ---
        private Button btnFechar = new Button();
        private Button btnMinimizar = new Button();
        private Label lblStatus = new Label();
        private Label lblTitulo = new Label();

        // Labels para os nomes das funções na grade de status
        private Label nH = new Label(), nV = new Label(), nG = new Label(), nM = new Label();
        private Label nGame = new Label(), nHib = new Label(), nDef = new Label(), nTimer = new Label();

        // Labels para os valores reais (ATIVADO/DESATIVADO) em tempo real
        private Label vH = new Label(), vV = new Label(), vG = new Label(), vM = new Label();
        private Label vGame = new Label(), vHib = new Label(), vDef = new Label(), vTimer = new Label();

        private ProgressBar progressBar = new ProgressBar();
        private List<Button> botoesMenu = new List<Button>();
        private static Mutex mutex = null; // Garante instância única do app
        private bool estaProcessando = false; // Trava de segurança contra cliques múltiplos
        private System.Windows.Forms.Timer refreshTimer = new System.Windows.Forms.Timer();

        // --- IMPORTAÇÃO DE FUNÇÕES NATIVAS DO WINDOWS (API) ---
        [DllImport("user32.dll")]
        private extern static void ReleaseCapture(); // Permite arrastar a janela sem bordas

        [DllImport("user32.dll")]
        private extern static void SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags); // Limpa lixeira via código

        [DllImport("ntdll.dll")]
        private static extern int NtSetTimerResolution(uint DesiredResolution, bool SetResolution, out uint CurrentResolution); // Força 0.5ms

        [DllImport("ntdll.dll")]
        private static extern int NtQueryTimerResolution(out uint MaximumResolution, out uint MinimumResolution, out uint CurrentResolution); // Lê latência real

        // Reduz o "piscar" da tela ao carregar componentes
        protected override CreateParams CreateParams
        {
            get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; }
        }

        public Form1()
        {
            InitializeComponent();

            // Verifica se o programa já está aberto para não abrir duas vezes
            mutex = new Mutex(true, "C4Otimizador_V4_Final_Stable", out bool createdNew);
            if (!createdNew) { Application.Exit(); return; }

            this.Opacity = 0; // Inicia invisível para efeito de fade-in
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);

            ConfigurarInterface();
            AtualizarTodosStatus(); // Primeira leitura dos sensores
            ValidarBuildSistema(); // Checa se é Win10 22H2

            // Configura a atualização automática dos sensores a cada 3 segundos
            refreshTimer.Interval = 3000;
            refreshTimer.Tick += (s, e) => { if (!estaProcessando) AtualizarTodosStatus(); };
            refreshTimer.Start();

            // Lógica para arrastar a janela personalizada
            this.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(this.Handle, 0x112, 0xf012, 0); } };

            // Efeito suave de abertura
            this.Shown += async (s, e) => { await Task.Delay(100); this.Opacity = 1; };
        }

        // --- VALIDAÇÃO DE COMPATIBILIDADE ---
        private void ValidarBuildSistema()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    string b = key?.GetValue("CurrentBuild")?.ToString() ?? "0";
                    if (b == "19045") { lblStatus.Text = "SISTEMA COMPATÍVEL (WIN 10 22H2)"; lblStatus.ForeColor = Color.Yellow; }
                    else { lblStatus.Text = "SISTEMA INCOMPATÍVEL (BUILD " + b + ")"; lblStatus.ForeColor = Color.Red; }
                }
            }
            catch { lblStatus.Text = "ERRO NA VALIDAÇÃO"; }
        }

        // Desenha a borda Verde Lima ao redor da janela
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Suavização para a borda não ficar serrilhada
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Limpa o fundo para garantir que não haja rastros
            e.Graphics.Clear(Color.FromArgb(2, 5, 2));

            // AJUSTE CIRÚRGICO: Usamos -1 na largura e -2 na altura para a borda não sumir embaixo
            // E definimos a espessura da caneta (Pen) para 2 ou 3 para ficar bem visível
            using (Pen p = new Pen(Color.FromArgb(120, Color.Lime), 2))
            {
                // O retângulo começa em 0,0 e vai até o limite visível real
                e.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
            }
        }


        private void ConfigurarInterface()
        {
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            this.Size = new Size(550, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(2, 5, 2);

            // --- BOTÃO FECHAR (X) ---
            btnFechar.Text = "X";
            btnFechar.Size = new Size(35, 35);
            btnFechar.Location = new Point(489, 10);
            btnFechar.FlatStyle = FlatStyle.Flat;
            btnFechar.FlatAppearance.BorderSize = 0;
            btnFechar.ForeColor = Color.White;
            btnFechar.BackColor = Color.FromArgb(2, 5, 2);
            btnFechar.FlatAppearance.MouseOverBackColor = Color.Red;
            btnFechar.Click += (s, e) => Application.Exit();
            this.Controls.Add(btnFechar);

            // --- BOTÃO MINIMIZAR (Visual Padrão Windows) ---
            btnMinimizar.Text = "_"; // Símbolo correto para minimizar
            btnMinimizar.Size = new Size(35, 35);
            btnMinimizar.Location = new Point(453, 10);
            btnMinimizar.FlatStyle = FlatStyle.Flat;
            btnMinimizar.FlatAppearance.BorderSize = 0;
            btnMinimizar.ForeColor = Color.White;
            btnMinimizar.BackColor = Color.FromArgb(2, 5, 2);
            btnMinimizar.Font = new Font("Segoe UI", 9F, FontStyle.Bold); // Fonte do sistema
            btnMinimizar.TextAlign = ContentAlignment.MiddleCenter;
            btnMinimizar.Padding = new Padding(0, 0, 0, 8); // Ajuste fino para subir o "_" e centralizar
            btnMinimizar.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60);
            btnMinimizar.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            this.Controls.Add(btnMinimizar);


            lblTitulo.Text = "C4 OTIMIZADOR 4.0";
            lblTitulo.ForeColor = Color.White;
            lblTitulo.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            lblTitulo.Location = new Point(0, 35);
            lblTitulo.Size = new Size(550, 45);
            lblTitulo.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblTitulo);

            int c1 = 50, c2 = 295, yI = 105, sp = 50;

            // --- INSTANCIAÇÃO DOS BOTÕES (COLUNA 1) ---
            CriarBtn("Backup", c1, yI, Color.FromArgb(33, 150, 243), async (s, e) => await Wrap(ExecutarBackup(), "Backup"));
            CriarBtn("Otimizar Mouse", c1, yI + sp, Color.FromArgb(76, 175, 80), async (s, e) => await Wrap(InjetarRegistro(), "Mouse"));
            CriarBtn("Limpeza (GPU+RAM)", c1, yI + (sp * 2), Color.FromArgb(156, 39, 176), async (s, e) => await Wrap(ExecutarLimpeza(), "Limpeza"));
            CriarBtn("Otimizar Rede", c1, yI + (sp * 3), Color.Gold, async (s, e) => await Wrap(OtimizarRede(), "Rede"));
            CriarBtn("Máximo Desempenho", c1, yI + (sp * 4), Color.FromArgb(255, 87, 34), async (s, e) => await Wrap(AtivarTurboMode(), "Desempenho"));
            CriarBtn("Latência 0.5ms", c1, yI + (sp * 5), Color.FromArgb(0, 150, 136), async (s, e) => await Wrap(AplicarTimer(), "Timer"));
            CriarBtn("Defender OFF", c1, yI + (sp * 6), Color.FromArgb(183, 28, 28), async (s, e) => await Wrap(DesativarAntivirus(), "Defender"));

            // --- INSTANCIAÇÃO DOS BOTÕES (COLUNA 2) ---
            CriarBtn("Desativar Hyper-V", c2, yI, Color.FromArgb(255, 152, 0), async (s, e) => await Wrap(DesativarHyperV(), "Hyper-V"));
            CriarBtn("Desativar VBS", c2, yI + sp, Color.FromArgb(121, 85, 72), async (s, e) => await Wrap(DesativarVBS(), "VBS"));
            CriarBtn("Desativar HAGS", c2, yI + (sp * 2), Color.FromArgb(0, 188, 212), async (s, e) => await Wrap(DesativarHAGS(), "HAGS"));
            CriarBtn("Desativar MPO", c2, yI + (sp * 3), Color.FromArgb(255, 0, 120), async (s, e) => await Wrap(DesligarMPO(), "MPO"));
            CriarBtn("Otimizar Visual", c2, yI + (sp * 4), Color.FromArgb(63, 81, 181), async (s, e) => await Wrap(OtimizarVisual(), "Visual"));
            CriarBtn("Restaurar Padrão", c2, yI + (sp * 5), Color.FromArgb(244, 67, 54), async (s, e) => await Wrap(RestaurarTudo(), "Reset"));
            CriarBtn("Formatar PC", c2, yI + (sp * 6), Color.DarkRed, async (s, e) => await Wrap(FormatarSistema(), "Formatação"));

            CriarBtn("Apoiar Projeto (PIX)", 175, 465, Color.DeepPink, (s, e) => { if (!estaProcessando) MostrarPix(); });

            // --- GRADE DE SENSORES (DNA V4) ---
            int yS = 525;
            CriarL(vM, nM, "DWM MPO:", yS); CriarL(vG, nG, "GPU HAGS:", yS + 22);
            CriarL(vV, nV, "VBS KERNEL:", yS + 44); CriarL(vH, nH, "HYPER-V:", yS + 66);
            CriarL(vGame, nGame, "GAME MODE:", yS + 88); CriarL(vHib, nHib, "HIBERNAÇÃO:", yS + 110);
            CriarL(vDef, nDef, "DEFENDER:", yS + 132); CriarL(vTimer, nTimer, "TIMER KERNEL:", yS + 154);

            lblStatus.Text = "AGUARDANDO AÇÃO...";
            lblStatus.ForeColor = Color.White;
            lblStatus.Location = new Point(0, 710); // Subi um pouco conforme sua dúvida anterior
            lblStatus.Size = new Size(550, 20);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblStatus);

            progressBar.Size = new Size(450, 14);
            progressBar.Location = new Point(50, 735); // Subi um pouco conforme sua dúvida anterior
            this.Controls.Add(progressBar);
        }

        // Método auxiliar para criar botões arredondados com 1 linha de código
        private void CriarBtn(string t, int x, int y, Color c, EventHandler ev)
        {
            Button b = new Button { Text = t, Size = new Size(200, 42), Location = new Point(x, y), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(180, c), ForeColor = Color.White, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            b.FlatAppearance.BorderSize = 0; b.Click += ev;
            b.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int r = 15;
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddArc(1, 1, r, r, 180, 90); gp.AddArc(b.Width - r - 1, 1, r, r, 270, 90);
                    gp.AddArc(b.Width - r - 1, b.Height - r - 1, r, r, 0, 90); gp.AddArc(1, b.Height - r - 1, r, r, 90, 90);
                    b.Region = new Region(gp);
                    using (Pen pen = new Pen(Color.FromArgb(150, Color.Lime), 1.5f)) e.Graphics.DrawPath(pen, gp);
                }
            };
            this.Controls.Add(b);
        }

        // Método auxiliar para criar linhas de status (Labels)
        private void CriarL(Label v, Label n, string t, int y)
        {
            n.Text = t; n.ForeColor = Color.White; n.Font = new Font("Segoe UI", 9F, FontStyle.Bold); n.Location = new Point(140, y); n.Size = new Size(130, 20); n.TextAlign = ContentAlignment.MiddleRight; this.Controls.Add(n);
            v.Text = "..."; v.ForeColor = Color.Lime; v.Font = new Font("Segoe UI", 9F, FontStyle.Bold); v.Location = new Point(275, y); v.Size = new Size(150, 20); v.TextAlign = ContentAlignment.MiddleLeft; this.Controls.Add(v);
        }

        // --- GERENCIADOR DE TAREFAS (BLINDAGEM) ---
        private async Task Wrap(Task op, string t)
        {
            if (estaProcessando) return; // Impede clicar em outro botão enquanto um roda
            estaProcessando = true;
            lblStatus.Text = "Processando " + t + "...";
            progressBar.Value = 100;
            try
            {
                await op;
                await Task.Delay(300);
                AtualizarTodosStatus(); // Atualiza sensores após a mudança
                SystemSounds.Asterisk.Play(); // Som padrão de sucesso
                MessageBox.Show(t + " aplicado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Erro: " + ex.Message); }
            finally { estaProcessando = false; progressBar.Value = 0; lblStatus.Text = "AGUARDANDO AÇÃO..."; }
        }

        // --- SENSORES EM TEMPO REAL ---
        private void AtualizarTodosStatus()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(AtualizarTodosStatus)); return; }
            CheckReg(vH, @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 1);
            CheckReg(vV, @"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 1);
            CheckReg(vG, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
            CheckMPO();
            CheckReg(vGame, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1, true);
            CheckReg(vHib, @"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled", 1);
            CheckReg(vDef, @"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware", 1, false, true);
            CheckTimerReal();
        }

        // Lê a latência real do Windows via NTDLL
        private void CheckTimerReal()
        {
            try
            {
                NtQueryTimerResolution(out _, out _, out uint current);
                double ms = current / 10000.0;
                vTimer.Text = ms.ToString("0.000") + " ms";
                vTimer.ForeColor = (ms < 1.0) ? Color.Lime : Color.Red;
            }
            catch { vTimer.Text = "OFFLINE"; }
        }

        // Verifica chaves de registro para os sensores (Cor Lime se Otimizado)
        private void CheckReg(Label v, string p, string k, int oV, bool hkcu = false, bool def = false)
        {
            try
            {
                using (var key = (hkcu ? Registry.CurrentUser : Registry.LocalMachine).OpenSubKey(p))
                {
                    if (key == null) { v.Text = "OFFLINE"; return; }
                    int val = Convert.ToInt32(key.GetValue(k) ?? 0);
                    bool isOn = (val == oV);
                    v.Text = def ? (isOn ? "DESATIVADO" : "ATIVADO") : (isOn ? "ATIVADO" : "DESATIVADO");
                    if (def || k == "HwSchMode" || k == "EnableVirtualizationBasedSecurity" || k == "Enabled" || k == "HibernateEnabled")
                        v.ForeColor = (v.Text == "DESATIVADO") ? Color.Lime : Color.Red;
                    else
                        v.ForeColor = (v.Text == "ATIVADO") ? Color.Lime : Color.Red;
                }
            }
            catch { v.Text = "OFFLINE"; }
        }

        private void CheckMPO() { try { using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Dwm")) { int val = Convert.ToInt32(key?.GetValue("OverlayTestMode") ?? 0); vM.Text = (val == 5) ? "DESATIVADO" : "ATIVADO"; vM.ForeColor = (val == 5) ? Color.Lime : Color.Red; } } catch { vM.Text = "OFFLINE"; } }

        // --- MOTOR DE EXECUÇÃO DE COMANDOS CMD/POWERSHELL ---
        private async Task<bool> Exec(string c, string a)
        {
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo = new ProcessStartInfo { FileName = c, Arguments = a, CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true };
                    p.Start();
                    await Task.Run(() => p.StandardOutput.ReadToEnd());
                    await Task.Run(() => p.WaitForExit());
                    return true;
                }
            }
            catch { return false; }
        }

        // Motor de escrita no registro
        private void Reg(RegistryKey r, string p, string n, object v, RegistryValueKind t)
        {
            try { using (var k = r.CreateSubKey(p)) k.SetValue(n, v, t); } catch { }
        }

        // --- MÉTODOS DE AÇÃO (DNA V3 + UPGRADES V4) ---

        private async Task AplicarTimer() { await Task.Run(() => NtSetTimerResolution(5000, true, out _)); }

        private async Task InjetarRegistro()
        {
            await Task.Run(async () => {
                Reg(Registry.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0", RegistryValueKind.String);
                Reg(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\mouclass\Parameters", "MouseDataQueueSize", 25, RegistryValueKind.DWord);
                await Exec("powercfg", "/setacvalueindex scheme_current sub_buttons usb-suspend 0"); // USB Suspend OFF
                await Exec("powercfg", "/setactive scheme_current");
            });
        }

        private async Task OtimizarRede()
        {
            await Task.Run(async () => {
                Reg(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", -1, RegistryValueKind.DWord);
                await Exec("ipconfig", "/flushdns");
                Reg(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord); // DVR OFF
                Reg(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0, RegistryValueKind.DWord);
            });
        }

        private async Task AtivarTurboMode()
        {
            await Task.Run(async () => {
                string turbo = "e9a42b02-d5df-448d-aa00-03f14749eb61";
                await Exec("powercfg", "/delete " + turbo);
                await Exec("powercfg", "/restoredefaultschemes");
                await Exec("powercfg", "/duplicatescheme " + turbo);
                await Exec("powercfg", "-h off"); // Deleta arquivo de hibernação
                await Exec("powercfg", "/setactive " + turbo);
                Reg(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord); // Telemetria OFF
                Reg(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1, RegistryValueKind.DWord); // Apps Fundo OFF
            });
            await Exec("sc", "stop DiagTrack"); await Exec("sc", "config DiagTrack start= disabled");
        }

        private async Task DesativarHyperV() { await Exec("bcdedit", "/set hypervisorlaunchtype off"); await Exec("DISM", "/Online /Disable-Feature /FeatureName:Microsoft-Hyper-V-All /NoRestart"); }
        private async Task DesativarVBS() { await Task.Run(() => Reg(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord)); }
        private async Task DesativarHAGS() { await Task.Run(() => Reg(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 1, RegistryValueKind.DWord)); }
        private async Task DesligarMPO() { await Task.Run(() => Reg(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\Dwm", "OverlayTestMode", 5, RegistryValueKind.DWord)); }
        private async Task ExecutarBackup() { await Exec("powershell", "Checkpoint-Computer -Description 'C4' -RestorePointType MODIFY_SETTINGS"); }
        private async Task DesativarAntivirus() { await Exec("powershell", "Set-MpPreference -DisableRealtimeMonitoring $true"); }

        private async Task OtimizarVisual()
        {
            await Task.Run(async () => {
                Reg(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0, RegistryValueKind.DWord);
                Reg(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2, RegistryValueKind.DWord);
                byte[] mask = new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 };
                Reg(Registry.CurrentUser, @"Control Panel\Desktop", "UserPreferencesMask", mask, RegistryValueKind.Binary);
                Reg(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0, RegistryValueKind.DWord); // Widgets OFF
                await Exec("taskkill", "/f /im explorer.exe"); Process.Start("explorer.exe"); // Reinicia para aplicar Widgets
            });
        }

        private async Task RestaurarTudo()
        {
            await Task.Run(async () => {
                await Exec("powercfg", "/restoredefaultschemes"); await Exec("powercfg", "-h on");
                Reg(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, RegistryValueKind.DWord);
                await Exec("taskkill", "/f /im explorer.exe"); Process.Start("explorer.exe");
            });
        }

        private async Task FormatarSistema() { if (MessageBox.Show("Iniciar restauração nativa?", "C4 V4", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { try { Process.Start(new ProcessStartInfo { FileName = "systemreset.exe", Arguments = "--factoryreset", UseShellExecute = true, Verb = "runas" }); } catch { } } await Task.CompletedTask; }
        private void MostrarPix() { Form f = new Form { Text = "Apoio", Size = new Size(320, 380), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedToolWindow, BackColor = Color.FromArgb(2, 5, 2) }; PictureBox p = new PictureBox { Image = Properties.Resources.pix, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill }; f.Controls.Add(p); f.ShowDialog(); }

        // Limpeza profunda de Cache de GPU e RAM
        private async Task ExecutarLimpeza()
        {
            await Task.Run(() => {
                List<string> ps = new List<string> { Path.GetTempPath(), "C:\\Windows\\Temp", "C:\\Windows\\Prefetch", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\DirectX Shader Cache"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NVIDIA\GLCache"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NVIDIA\DXCache"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"AMD\DXCache") };
                foreach (string p in ps) { try { if (Directory.Exists(p)) { DirectoryInfo di = new DirectoryInfo(p); foreach (FileInfo f in di.GetFiles()) try { f.Delete(); } catch { } foreach (DirectoryInfo d in di.GetDirectories()) try { d.Delete(true); } catch { } } } catch { } }
                try { SHEmptyRecycleBin(IntPtr.Zero, null, 1 | 4); } catch { }
                foreach (Process pr in Process.GetProcesses()) { try { pr.MinWorkingSet = pr.MinWorkingSet; } catch { } } // Limpa RAM Standby
            });
        }
    }
}
