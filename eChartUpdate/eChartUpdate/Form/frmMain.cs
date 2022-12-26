namespace eChartUpdate.Form
{
    public partial class frmMain : System.Windows.Forms.Form
    {
        public frmMain()
        {
            InitializeComponent();
            Load += frmMain_Load;

            //Logging Gubun
            //A : 텍스트 파일 생성하여 표기
            //그 외 : 화면 textBox 에 표기
            _Logging = "A";
        }        

        private void frmMain_Load(object? sender, EventArgs e)
        {
            var frmForm = new frmUpdate(_Logging);
            frmForm.ShowDialog();

            this.Close();
        }        

        private readonly string _Logging;   
    }
}
