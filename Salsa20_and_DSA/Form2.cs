using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Salsa20_and_DSA
{
    public sealed class Salsa20 : SymmetricAlgorithm
    {
        
        public Salsa20()
        {
            LegalBlockSizesValue = new[] { new KeySizes(512, 512, 0) };
            LegalKeySizesValue = new[] { new KeySizes(128, 256, 128) };

            BlockSizeValue = 512;
            KeySizeValue = 256;
            m_rounds = 20;
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return CreateEncryptor(rgbKey, rgbIV);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            if (rgbKey == null)
                throw new ArgumentNullException("rgbKey");
            if (!ValidKeySize(rgbKey.Length * 8))
                throw new CryptographicException("Invalid key size; it must be 128 or 256 bits.");
            CheckValidIV(rgbIV, "rgbIV");

            return new Salsa20CryptoTransform(rgbKey, rgbIV, m_rounds);
        }

        public override void GenerateIV()
        {
            IVValue = GetRandomBytes(8);
        }

        public override void GenerateKey()
        {
            KeyValue = GetRandomBytes(KeySize / 8);
        }

        public override byte[] IV
        {
            get
            {
                return base.IV;
            }
            set
            {
                CheckValidIV(value, "value");
                IVValue = (byte[])value.Clone();
            }
        }

        public int Rounds
        {
            get
            {
                return m_rounds;
            }
            set
            {
                if (value != 8 && value != 12 && value != 20)
                    throw new ArgumentOutOfRangeException("value", "The number of rounds must be 8, 12, or 20.");
                m_rounds = value;
            }
        }

        private static void CheckValidIV(byte[] iv, string paramName)
        {
            if (iv == null)
                throw new ArgumentNullException(paramName);
            if (iv.Length != 8)
                throw new CryptographicException("Invalid IV size; it must be 8 bytes.");
        }

        private static byte[] GetRandomBytes(int byteCount)
        {
            byte[] bytes = new byte[byteCount];
            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
                rng.GetBytes(bytes);
            return bytes;
        }

        int m_rounds;

        private sealed class Salsa20CryptoTransform : ICryptoTransform
        {
            public Salsa20CryptoTransform(byte[] key, byte[] iv, int rounds)
            {
                Debug.Assert(key.Length == 16 || key.Length == 32, "abyKey.Length == 16 || abyKey.Length == 32", "Invalid key size.");
                Debug.Assert(iv.Length == 8, "abyIV.Length == 8", "Invalid IV size.");
                Debug.Assert(rounds == 8 || rounds == 12 || rounds == 20, "rounds == 8 || rounds == 12 || rounds == 20", "Invalid number of rounds.");

                Initialize(key, iv);
                m_rounds = rounds;
            }

            public bool CanReuseTransform
            {
                get { return false; }
            }

            public bool CanTransformMultipleBlocks
            {
                get { return true; }
            }

            public int InputBlockSize
            {
                get { return 64; }
            }

            public int OutputBlockSize
            {
                get { return 64; }
            }

            public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
            {
                if (inputBuffer == null)
                    throw new ArgumentNullException("inputBuffer");
                if (inputOffset < 0 || inputOffset >= inputBuffer.Length)
                    throw new ArgumentOutOfRangeException("inputOffset");
                if (inputCount < 0 || inputOffset + inputCount > inputBuffer.Length)
                    throw new ArgumentOutOfRangeException("inputCount");
                if (outputBuffer == null)
                    throw new ArgumentNullException("outputBuffer");
                if (outputOffset < 0 || outputOffset + inputCount > outputBuffer.Length)
                    throw new ArgumentOutOfRangeException("outputOffset");
                if (m_state == null)
                    throw new ObjectDisposedException(GetType().Name);

                byte[] output = new byte[64];
                int bytesTransformed = 0;

                while (inputCount > 0)
                {
                    Hash(output, m_state);
                    m_state[8] = AddOne(m_state[8]);
                    if (m_state[8] == 0)
                    {

                        m_state[9] = AddOne(m_state[9]);
                    }

                    int blockSize = Math.Min(64, inputCount);
                    for (int i = 0; i < blockSize; i++)
                        outputBuffer[outputOffset + i] = (byte)(inputBuffer[inputOffset + i] ^ output[i]);
                    bytesTransformed += blockSize;

                    inputCount -= 64;
                    outputOffset += 64;
                    inputOffset += 64;
                }

                return bytesTransformed;
            }

            public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
            {
                if (inputCount < 0)
                    throw new ArgumentOutOfRangeException("inputCount");

                byte[] output = new byte[inputCount];
                TransformBlock(inputBuffer, inputOffset, inputCount, output, 0);
                return output;
            }

            public void Dispose()
            {
                if (m_state != null)
                {
                    Array.Clear(m_state, 0, m_state.Length);
                }
                m_state = null;
            }

            private static uint Rotate(uint v, int c)
            {
                return (v << c) | (v >> (32 - c));
            }

            private static uint Add(uint v, uint w)
            {
                return unchecked(v + w);
            }

            private static uint AddOne(uint v)
            {
                return unchecked(v + 1);
            }

            private void Hash(byte[] output, uint[] input)
            {
                uint[] state = (uint[])input.Clone();

                for (int round = m_rounds; round > 0; round -= 2)
                {
                    state[4] ^= Rotate(Add(state[0], state[12]), 7);
                    state[8] ^= Rotate(Add(state[4], state[0]), 9);
                    state[12] ^= Rotate(Add(state[8], state[4]), 13);
                    state[0] ^= Rotate(Add(state[12], state[8]), 18);
                    state[9] ^= Rotate(Add(state[5], state[1]), 7);
                    state[13] ^= Rotate(Add(state[9], state[5]), 9);
                    state[1] ^= Rotate(Add(state[13], state[9]), 13);
                    state[5] ^= Rotate(Add(state[1], state[13]), 18);
                    state[14] ^= Rotate(Add(state[10], state[6]), 7);
                    state[2] ^= Rotate(Add(state[14], state[10]), 9);
                    state[6] ^= Rotate(Add(state[2], state[14]), 13);
                    state[10] ^= Rotate(Add(state[6], state[2]), 18);
                    state[3] ^= Rotate(Add(state[15], state[11]), 7);
                    state[7] ^= Rotate(Add(state[3], state[15]), 9);
                    state[11] ^= Rotate(Add(state[7], state[3]), 13);
                    state[15] ^= Rotate(Add(state[11], state[7]), 18);
                    state[1] ^= Rotate(Add(state[0], state[3]), 7);
                    state[2] ^= Rotate(Add(state[1], state[0]), 9);
                    state[3] ^= Rotate(Add(state[2], state[1]), 13);
                    state[0] ^= Rotate(Add(state[3], state[2]), 18);
                    state[6] ^= Rotate(Add(state[5], state[4]), 7);
                    state[7] ^= Rotate(Add(state[6], state[5]), 9);
                    state[4] ^= Rotate(Add(state[7], state[6]), 13);
                    state[5] ^= Rotate(Add(state[4], state[7]), 18);
                    state[11] ^= Rotate(Add(state[10], state[9]), 7);
                    state[8] ^= Rotate(Add(state[11], state[10]), 9);
                    state[9] ^= Rotate(Add(state[8], state[11]), 13);
                    state[10] ^= Rotate(Add(state[9], state[8]), 18);
                    state[12] ^= Rotate(Add(state[15], state[14]), 7);
                    state[13] ^= Rotate(Add(state[12], state[15]), 9);
                    state[14] ^= Rotate(Add(state[13], state[12]), 13);
                    state[15] ^= Rotate(Add(state[14], state[13]), 18);
                }

                for (int index = 0; index < 16; index++)
                    ToBytes(Add(state[index], input[index]), output, 4 * index);
            }

            private void Initialize(byte[] key, byte[] iv)
            {
                m_state = new uint[16];
                m_state[1] = ToUInt32(key, 0);
                m_state[2] = ToUInt32(key, 4);
                m_state[3] = ToUInt32(key, 8);
                m_state[4] = ToUInt32(key, 12);

                byte[] constants = key.Length == 32 ? c_sigma : c_tau;
                int keyIndex = key.Length - 16;

                m_state[11] = ToUInt32(key, keyIndex + 0);
                m_state[12] = ToUInt32(key, keyIndex + 4);
                m_state[13] = ToUInt32(key, keyIndex + 8);
                m_state[14] = ToUInt32(key, keyIndex + 12);
                m_state[0] = ToUInt32(constants, 0);
                m_state[5] = ToUInt32(constants, 4);
                m_state[10] = ToUInt32(constants, 8);
                m_state[15] = ToUInt32(constants, 12);

                m_state[6] = ToUInt32(iv, 0);
                m_state[7] = ToUInt32(iv, 4);
                m_state[8] = 0;
                m_state[9] = 0;
            }

            private static uint ToUInt32(byte[] input, int inputOffset)
            {
                return unchecked((uint)(((input[inputOffset] | (input[inputOffset + 1] << 8)) | (input[inputOffset + 2] << 16)) | (input[inputOffset + 3] << 24)));
            }

            private static void ToBytes(uint input, byte[] output, int outputOffset)
            {
                unchecked
                {
                    output[outputOffset] = (byte)input;
                    output[outputOffset + 1] = (byte)(input >> 8);
                    output[outputOffset + 2] = (byte)(input >> 16);
                    output[outputOffset + 3] = (byte)(input >> 24);
                }
            }

            static readonly byte[] c_sigma = Encoding.ASCII.GetBytes("expand 32-byte k");
            static readonly byte[] c_tau = Encoding.ASCII.GetBytes("expand 16-byte k");

            uint[] m_state;
            readonly int m_rounds;
        }
    }
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Secret Key Generation Button
            if (textBox1.Text != "" && textBox2.Text != "")
            {
                int p = Convert.ToInt32(textBox1.Text);
                int q = Convert.ToInt32(textBox2.Text);
                int g = Convert.ToInt32(Math.Pow(2, ((p - 1) / q)) % p);
                Random rnd = new Random();
                int x = rnd.Next(1, q - 1); //Закрытый ключ
                int y = Convert.ToInt32(Math.Pow(g, x) % p); //Открытый ключ
                textBox3.Text = x.ToString();
                button1.Enabled = false;
                richTextBox3.AppendText("Log > Генерация секретного ключа прошла успешно. \n");
            }
            else
            {
                try
                {
                    richTextBox2.AppendText("Ошибка #0005. Не сгенерированы числа p и q! \n");
                }
                catch
                {
                    richTextBox2.AppendText("Ошибка #0005. Не сгенерированы числа p и q!  \n");
                }
            }

        }
        private void button2_Click(object sender, EventArgs e)
        {
            //Public Key Generation

            if (textBox1.Text != "" && textBox2.Text != "")
            {
                int p = Convert.ToInt32(textBox1.Text);
                int q = Convert.ToInt32(textBox2.Text);
                int g = Convert.ToInt32(Math.Pow(2, ((p - 1) / q)) % p);
                Random rnd = new Random();
                int x = rnd.Next(1, q - 1); //Закрытый ключ
                int y = Convert.ToInt32(Math.Pow(g, x) % p); //Открытый ключ
                textBox4.Text = y.ToString();
                textBox5.Text = g.ToString();
                richTextBox3.AppendText("Log > Вычисление значения G прошло успешно. \n");
                button2.Enabled = false;
                richTextBox3.AppendText("Log > Генерация публичного ключа прошла успешно. \n");
                if (textBox1.Text != "" && textBox2.Text != "" && textBox3.Text != "" && textBox4.Text != "")
                {
                    button3.Enabled = true;
                }
            }
            else
            {
                try
                {
                    richTextBox2.AppendText("Ошибка #0005. Не сгенерированы числа p и q! \n");
                }
                catch
                {
                    richTextBox2.AppendText("Ошибка #0005. Не сгенерированы числа p и q!  \n");
                }
            }
        }

        private void label5_Click(object sender, EventArgs e)
        {
            //Useless 
        }

        private void GenerateP_Click(object sender, EventArgs e)
        {
            //Создание числа P зависящего от Q
            button5.Enabled = true;
            button3.Enabled = false;
            Random rnd = new Random();
            int p = (Convert.ToInt32(textBox2.Text)) * rnd.Next(2, 100) + 1;
            textBox1.Text = p.ToString();
        }
        private void GenerateQ_Click(object sender, EventArgs e)
        {
            //Создание числа Q 64-127
            button4.Enabled = false;
            button9.Enabled = false;
            button2.Enabled = true;
            button6.Enabled = true;
            button3.Enabled = false;
            Random rnd = new Random();
            int q = rnd.Next(64, 127);
            textBox2.Text = q.ToString();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //Проверка числа P
            if (textBox1.Text != "")
            {
                richTextBox1.Text = "";
                int p = Convert.ToInt32(textBox1.Text);
                bool isPrime = true;
                for (int i = 2; i <= Math.Sqrt(p); i++)
                {
                    if (p % i == 0)
                    {
                        isPrime = false;
                        break;
                    }
                }
                if (isPrime)
                {
                    richTextBox1.Text = "";
                    richTextBox3.AppendText("Log > Число P было проверено на простоту.Тест пройден. \n");
                    button5.Enabled = false;
                }
                else
                {
                    richTextBox1.Text = "";
                    richTextBox2.AppendText("Ошибка #0001. Число P не прошло проверку на простоту. Будет сгенерировано другое число. \n");
                    Random rnd = new Random();
                    int pg = (Convert.ToInt32(textBox2.Text)) * rnd.Next(2, 100) + 1;
                    textBox1.Text = pg.ToString();
                    richTextBox1.AppendText(Text);
                }
            }
            else
            {
                try
                {
                    richTextBox2.AppendText("Ошибка #0003. Не сгенерировано число P!  \n");
                    button5.Enabled = false;
                }
                catch
                {
                    richTextBox2.AppendText("Ошибка #0003. Не сгенерировано число P!   \n");
                    button5.Enabled = false;
                }
            }


        }

        private void button6_Click(object sender, EventArgs e)
        {
            //Проверка числа Q
            if (textBox2.Text != "")
            {
                richTextBox1.Text = "";
                int q = Convert.ToInt32(textBox2.Text);
                bool isPrime = true;
                for (int i = 2; i <= Math.Sqrt(q); i++)
                {
                    if (q % i == 0)
                    {
                        isPrime = false;
                        break;
                    }
                }
                if (isPrime)
                {
                    richTextBox1.Text = "";
                    richTextBox3.AppendText("Log > Число Q было проверено на простоту.Тест пройден. \n");
                    button6.Enabled = false;
                    GenerateP.Enabled = true;
                }
                else
                {
                    richTextBox1.Text = "";
                    richTextBox2.AppendText("Ошибка #0002. Число Q не прошло проверку на простоту. Будет сгенерировано другое число. \n");
                    Random rnd = new Random();
                    int pq = rnd.Next(64, 127);
                    textBox2.Text = pq.ToString();
                }
            }
            else
            {
                try
                {
                    richTextBox2.AppendText("Ошибка #0004. Не сгенерировано число Q!   \n");
                    button6.Enabled = false;
                }
                catch
                {
                    richTextBox2.AppendText("Ошибка #0004. Не сгенерировано число Q!   \n");
                    button6.Enabled = false;
                }

            }

        }

        private void Form2_Load(object sender, EventArgs e)
        {
            //Useless 
        }

        private void label3_Click(object sender, EventArgs e)
        {
            //Useless
        }
        string hash;
        private void button8_Click(object sender, EventArgs e)
        {
            int k = 0;
            int r = 0;
            int s = 0;
            int h = 0;
            int q = Convert.ToInt32(textBox2.Text);
            int g = Convert.ToInt32(textBox5.Text);
            int p = Convert.ToInt32(textBox1.Text);
            int x = Convert.ToInt32(textBox3.Text);
            string text = Convert.ToString(richTextBox1.Text);
            byte[] bytes = Encoding.Default.GetBytes(text);
            string encoded_str = Encoding.UTF8.GetString(bytes);
            byte[] data = Encoding.Default.GetBytes(encoded_str);
            var result = new SHA256Managed().ComputeHash(data);
            string hashsha256 = BitConverter.ToString(result).Replace("-", "").ToLower();
            //richTextBox1.Text = hashsha256;
            richTextBox3.AppendText("Log > Хэш сообщения сгенерирован успешно. Хэш = " + hashsha256 + "\n");
            hash = hashsha256;
            int h1 = Convert.ToInt32(hashsha256.Length);
            string val = ToBinary(h1);
            int countTrim = 0;
            for (int i = 0; i < val.Length + 1; i++)
            {
                if (val[i] != '1')
                {
                    countTrim++;
                }
                else
                {
                    break;
                }
            }
            val = val.Remove(0, countTrim);
            //richTextBox1.AppendText("\n" + val + "\n");
            //richTextBox1.AppendText(Convert.ToString(val.Length));
            h = val.Length;
            textBox8.Text = Convert.ToString(h);
            int[] res1 = new int[2];
            res1 = whCycle(r, s, g, k, q, h, p, x);
            r = res1[0];
            s = res1[1];
            textBox6.Text = Convert.ToString(r);
            textBox7.Text = Convert.ToString(s);
            richTextBox3.AppendText("Log > Подпись успешно сгенерирована. \n");
        }

        public static string ToBinary(int x)
        {
            char[] buff = new char[32];

            for (int i = 31; i >= 0; i--)
            {
                int mask = 1 << i;
                buff[31 - i] = (x & mask) != 0 ? '1' : '0';
            }

            return new string(buff);
        }
        public static int[] whCycle(int r, int s, int g, int k, int q, int h, int p, int x)
        {
            bool f = true;
            while (f == true)
            {
                Random rnd = new Random();
                k = rnd.Next(0, q - 1);
                r = Convert.ToInt32((Math.Pow(g, k) % p) % q);
                if (r != 0)
                {
                    s = Convert.ToInt32((Math.Pow(k, -1) % q) * (h + x * r) % q);
                    if (s != 0)
                    {
                        f = false;
                        break;
                    }
                }
            }
            int[] res = new int[2];
            res[0] = r;
            res[1] = s;
            return res;

        }

        private void button9_Click(object sender, EventArgs e)
        {
            //Проверка подписи и хэша
            //Отключено

        }

        
        private void button3_Click(object sender, EventArgs e)
        {
            //Отправка переменных Q,P,G,text,hashed message,open key,подпись
            CallBackMy.callbackEventHandler(Convert.ToString(textBox1.Text), Convert.ToString(textBox2.Text), Convert.ToString(textBox5.Text),
                Convert.ToString(richTextBox1.Text),Convert.ToString(hash),Convert.ToString(textBox4.Text), Convert.ToString(textBox6.Text), Convert.ToString(textBox7.Text), Convert.ToString(textBox8.Text));


        }
        private void button7_Click(object sender, EventArgs e)
        {
            //Шифровка
            string text = richTextBox1.Text;
            byte[] key = { 117, 211, 146, 162, 230, 86, 207, 172, 3, 183, 170, 209, 16, 176, 21, 236, 94, 99, 85, 105, 120, 80, 208, 113, 59, 79, 207, 45, 198, 87, 227, 161 };
            byte[] iv = { 184, 68, 238, 192, 150, 147, 73, 174 };
            string encrypted;
            using (var salsa = new Salsa20())
            using (var mstream_out = new MemoryStream())
            {
                salsa.Key = key;
                salsa.IV = iv;

                using (var cstream = new CryptoStream(mstream_out, salsa.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    var bytes = Encoding.UTF8.GetBytes(text);
                    cstream.Write(bytes, 0, bytes.Length);
                }
                encrypted = Convert.ToBase64String(mstream_out.ToArray());
            }
            richTextBox1.Clear();
            richTextBox1.AppendText(encrypted);
            richTextBox3.AppendText("Log > Сообщение было зашифровано. \n");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //Расшифровка Отключена
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
            richTextBox3.AppendText("Log > Сообщение было расшифровано. \n");
        }
    }
}
