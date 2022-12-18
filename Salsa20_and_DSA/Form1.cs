using static System.Net.WebRequestMethods;

namespace Salsa20_and_DSA
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        { 
            Form2 fSend = new Form2();
            Form3 fReciever = new Form3();
            fSend.Show();
            fReciever.Show();
            this.Hide();
        }
    }
}