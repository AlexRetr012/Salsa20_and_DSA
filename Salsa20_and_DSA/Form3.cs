using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Salsa20_and_DSA
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
            CallBackMy.callbackEventHandler = new CallBackMy.callbackEvent(this.Reload);
        }
        string h1;
        void Reload(string p, string q, string g, string text,
            string hashtext, string openkey, string r, string s,string h)
        {
            textBox1.Text = p;
            richTextBox2.AppendText("Log > Получено значение P! \n");
            textBox2.Text = q;
            richTextBox2.AppendText("Log > Получено значение Q! \n");
            textBox3.Text = openkey;
            richTextBox2.AppendText("Log > Получен открытый ключ \n");
            textBox4.Text = g;
            richTextBox2.AppendText("Log > Получено значение G \n");
            textBox7.Text = hashtext;
            richTextBox2.AppendText("Log > Получен хэш сообщения! \n");
            textBox5.Text = r;
            textBox6.Text = s;
            richTextBox2.AppendText("Log > Данные подписи получены успешно \n");
            richTextBox1.Text = text;
            h1 = h;
        }

        private void label2_Click(object sender, EventArgs e)
        {
            
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Расшифровка
            byte[] key = { 117, 211, 146, 162, 230, 86, 207, 172, 3, 183, 170, 209, 16, 176, 21, 236, 94, 99, 85, 105, 120, 80, 208, 113, 59, 79, 207, 45, 198, 87, 227, 161 };
            byte[] iv = { 184, 68, 238, 192, 150, 147, 73, 174 };
            string decryptedText;
            using (var salsa = new Salsa20())
            using (var mstream_out = new MemoryStream())
            {
                salsa.Key = key;
                salsa.IV = iv;

                using (var mstream_in = new MemoryStream(Convert.FromBase64String(richTextBox1.Text)))
                using (var cstream = new CryptoStream(mstream_in, salsa.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    cstream.CopyTo(mstream_out);
                }
                decryptedText = Encoding.UTF8.GetString(mstream_out.ToArray());
            }
            richTextBox1.Clear();
            richTextBox1.AppendText(decryptedText);
            richTextBox2.AppendText("Log > Сообщение было расшифровано. \n");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string text = Convert.ToString(richTextBox1.Text);
            byte[] bytes = Encoding.Default.GetBytes(text);
            string encoded_str = Encoding.UTF8.GetString(bytes);
            byte[] data = Encoding.Default.GetBytes(encoded_str);
            var result = new SHA256Managed().ComputeHash(data);
            string hashsha256 = BitConverter.ToString(result).Replace("-", "").ToLower();
            richTextBox1.Text = hashsha256;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string hash1 = Convert.ToString(richTextBox1.Text);
            string hash2 = Convert.ToString(textBox7.Text);
            if(hash2 == hash1) 
            {
                richTextBox2.AppendText("Log > Сверка Хэшей прошла успешно! \n");
            }
            else
            {
               richTextBox2.AppendText("ERROR(#0008) > Хэши не совпадают! \n");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            int w;
            int u1;
            int u2;
            int v;
            int g = Convert.ToInt32(textBox4.Text);
            int p = Convert.ToInt32(textBox1.Text);
            int q = Convert.ToInt32(textBox2.Text);
            int r = Convert.ToInt32(textBox5.Text);
            int s = Convert.ToInt32(textBox6.Text);
            int y = Convert.ToInt32(textBox3.Text);
            int h2 = Convert.ToInt32(h1);
            int w1 = Convert.ToInt32(Math.Pow(3,-1));
            int w2 = Convert.ToInt32(w1 % 13);
            v = r;
            w =Convert.ToInt32(Math.Pow(g, s)%p);
            u1 = (Convert.ToInt32(h2) * w) % q;
            richTextBox1.AppendText(Convert.ToString(u1) + "\n");
            u2 = (r*w) % q;
            richTextBox1.AppendText(Convert.ToString(u2) + "\n");
            v =Convert.ToInt32
            (((Math.Pow(g, u1) * Math.Pow(y,u2) ) % p) %q);
            richTextBox1.AppendText(Convert.ToString(v + "\n"));
            if (v==r)
            {
            richTextBox2.AppendText("Log > Подпись верна! \n");
            }
            else
            {
            richTextBox2.AppendText("ERROR(#0010) > Подпись не верна \n");
            }
        }
    }
}
