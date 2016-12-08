﻿using System;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.IO;
using System.Text;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json;

namespace Auth
{
    public partial class Encrypt : Form
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            Label textLabel = new Label() { Left = 50, Top = 20, Text = text };
            TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
            Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 70, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";

        }


        System.Security.Cryptography.CspParameters cspp = new System.Security.Cryptography.CspParameters();
        System.Security.Cryptography.RSACryptoServiceProvider rsa;

        const string EncrFolder = @"c:\Encrypt\";
        const string DecrFolder = @"c:\Decrypt\";
        const string SrcFolder = @"c:\docs\";
    
        public string pubKey;
        public string privKey;

        public enum KeySizes
        {
            SIZE_512 = 512,
            SIZE_1024 = 1024,
            SIZE_2048 = 2048,
            SIZE_952 = 952,
            SIZE_1369 = 1369
        };

        //  const string keyName = "Key01";
        string keyName = DataContainer.User.ToString();

        // Public key file
        string PubKeyFile = @"c:\Encrypt\rsaPublicKey_";

        public Encrypt()
        {
            InitializeComponent();
        }

        //create the public and private keys, store in container
        private void buttonCreateAsmKeys_Click(object sender, EventArgs e)
        {

            DialogResult dialogResult = MessageBox.Show("This will overwrite any existing keys for " + DataContainer.User.ToString() + ". Do you want to continue?", "WARNING", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                MessageBox.Show("New KeySet Created");
                cspp.KeyContainerName = DataContainer.User;
               
                rsa = new RSACryptoServiceProvider(2048, cspp);
                //store a key pair in the key container.
                
                rsa.PersistKeyInCsp = true;
                if (rsa.PublicOnly == true)
                    label1.Text = "Key: " + cspp.KeyContainerName + " - Public Only";
                else
                    label1.Text = "Key: " + cspp.KeyContainerName + " - Full Key Pair";


                string promptValue = Encrypt.ShowDialog("Enter a file name", "New File");
                // MessageBox.Show(promptValue);

                string keyFileName = PubKeyFile + promptValue + ".txt";

                Directory.CreateDirectory(EncrFolder);
                StreamWriter sw = new StreamWriter(keyFileName, false);
                sw.Write(rsa.ToXmlString(false));
                sw.Close();
                MessageBox.Show("Public Key Exported to:" + keyFileName);

            }
            else if (dialogResult == DialogResult.No)
            {
                //do nothing
            }

        }

        private void buttonEncryptFile_Click(object sender, EventArgs e)
        {
            EncrytorM();
            conversationsCreate2(DataContainer.messageToUser.ToString());
            textBox1.Clear();
            this.Hide();
        }
        string sending2;
        void EncrytorM()
        {
            try
            {
                byte[] data = Encrypt9(Encoding.UTF8.GetBytes(textBox1.Text), ref pubKey);
                sending2 = Convert.ToBase64String(data);
            }
            catch
            {
                MessageBox.Show("Set a Public Key First");
            }
        
        }
        static void generateKeys()
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false; //Don't store keys in a key contrainer
                DataContainer.publicKey = rsa.ExportParameters(false);
                DataContainer.privateKey = rsa.ExportParameters(true);
            }
        }
        static byte[] Encrypt9(byte[] input, ref string key2)
        {
            byte[] encrypted;
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;
                try
                {
                    rsa.ImportParameters(DataContainer.publicKey);
                    encrypted = rsa.Encrypt(input, true);
                    key2 = rsa.ToXmlString(false);

                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    encrypted = null;
                    MessageBox.Show("No Public Key Set");
                }

            }

            return encrypted;
        }

        static byte[] Dencrypt9(byte[] input, ref string key2)
        {
            byte[] dencrypted;
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.PersistKeyInCsp = false;
                rsa.ImportParameters(DataContainer.privateKey);
                try
                {
                    dencrypted = rsa.Decrypt(input, true);
                }
                catch
                {
                    MessageBox.Show("Incorrect Private Key");
                    dencrypted = null;
                }

                key2 = rsa.ToXmlString(true);
            }
            return dencrypted;
        }


        string conversationID;
        async void conversationsCreate2(string email)
        {
            string sending = sending2;
            string myToken = DataContainer.ValueToShare.ToString();
            string messageTo = DataContainer.messageToUser.ToString();
            var builtUrl = new Url("https://chronoschat.co/conversations/create");
            var client2 = builtUrl
                .WithOAuthBearerToken(myToken);
            var resp = await client2
                .WithHeader("Accept", "application/json")
                .PostUrlEncodedAsync(new
                {
                    recipient_email = email
                })
                .ReceiveString()
                ;

            string output = resp.ToString();
            listAllUsers messageID = JsonConvert.DeserializeObject<listAllUsers>(output);
            string output2 = messageID.ToString();
            conversationID = messageID.id.ToString();

            var builtUrl2 = new Url("https://chronoschat.co/messages/create");

            var client3 = builtUrl2
                .WithOAuthBearerToken(myToken);

            try
            {
                var resp2 = await client3
                  .WithHeader("Accept", "application/json")
                  .PostUrlEncodedAsync(new
                  {
                      body = sending,
                      conversation_id = conversationID
                  })
                  .ReceiveString()
                  ;



                string resp2Output = resp2.ToString();
                listAllUsers sendOutput = JsonConvert.DeserializeObject<listAllUsers>(resp2Output);
                string output3 = sendOutput.ToString();
                string sendStatus = sendOutput.status.ToString();
                MessageBox.Show(sendStatus);
                if (sendStatus == "Message Sent")
                {
                    // textBox1.Clear();
                }
                else
                {
                    MessageBox.Show("An unknown error occured. Please try again later");
                }
            }
            catch
            {
                MessageBox.Show("An Error Occured. Message Not Sent.");
            }
        }

     
        private void buttonExportPublicKey_Click(object sender, EventArgs e)
        {
      
            string promptValue = Encrypt.ShowDialog("Enter a file name", "New File");
            string keyFileName = PubKeyFile + promptValue + ".txt";
            Directory.CreateDirectory(EncrFolder);
            StreamWriter sw = new StreamWriter(keyFileName, false);
            sw.Write(rsa.ToXmlString(false));
            sw.Close();
            MessageBox.Show("Public Key Exported to:" + keyFileName);
        }

        private void buttonImportPublicKey_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = SrcFolder;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fName = openFileDialog1.FileName;
                if (fName != null)
                {
                    FileInfo fInfo = new FileInfo(fName);
                    // Pass the file name without the path.
                    string name = fInfo.FullName;
                    StreamReader sr = new StreamReader(name);
                    cspp.KeyContainerName = keyName;
                    rsa = new RSACryptoServiceProvider(2048, cspp);

                    string keytxt = sr.ReadToEnd();
                    rsa.FromXmlString(keytxt);
                    rsa.PersistKeyInCsp = true;
                    DataContainer.publicKey = rsa.ExportParameters(false);
                    if (rsa.PublicOnly == true)
                        label1.Text = "Key: Public Only";
                    else
                        label1.Text = "Key: Full Key Pair for: " + cspp.KeyContainerName;
                    sr.Close();

                }
            }
        }
        private void buttonGetPrivateKey_Click(object sender, EventArgs e)
        {
            cspp.KeyContainerName = keyName;

            rsa = new RSACryptoServiceProvider(2048, cspp);
            DataContainer.privateKey = rsa.ExportParameters(true);

            rsa.PersistKeyInCsp = true;

            if (rsa.PublicOnly == true)
                label1.Text = "Key: Public Only";
            else
                label1.Text = "Key: Full Key Pair for: " + cspp.KeyContainerName;

        }

        private void Encrypt_Load(object sender, EventArgs e)
        {
            label2.Text = "Sending Encrypted Message to: " + DataContainer.messageToUser.ToString();
            label3.Text = "Logged in as: " + DataContainer.User.ToString();

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}