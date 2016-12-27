using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SteamTradeEmailActivator
{
    public partial class Form1 : Form
    {
        private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
        private static readonly string ExecutableFile = Assembly.Location;
        private static readonly string ExecutableDirectory = Path.GetDirectoryName(ExecutableFile);
        private int email_counter = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            label_status.ForeColor = Color.Blue;
            label_status.Text = "In Progress...";

            string error_path = Path.Combine(ExecutableDirectory, "error");
            string emails_path = Path.Combine(ExecutableDirectory, "emails");

            if (!Directory.Exists(error_path))
            {
                Directory.CreateDirectory(error_path);
            }
            if (!Directory.Exists(emails_path))
            {
                Directory.CreateDirectory(emails_path);
            }

            var emails = new Glob(Path.Combine(emails_path, "*.*"));
            string email_html = "";

            foreach (var val in emails.Expand().ToList())
            {
                string email_body = File.ReadAllText(val.FullName);

                Regex r = new Regex("(http\\:\\/\\/steamcommunity\\.com\\/email\\/TradeConfirmation\\?sparams\\=[^>' \"]+)");
                Match m = r.Match(email_body);
                if (m.Success)
                {
                    webBrowser1.Navigate(new Uri(m.Groups[1].ToString()));
                    while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                    {
                        Application.DoEvents();
                    }
                    //MessageBox.Show(webBrowser1.Url.ToString());
                    email_html = webBrowser1.Document.Body.InnerHtml.Replace("&amp;", "&");
                    //MessageBox.Show(html);
                }
                else
                {
                    File.Move(val.FullName, Path.Combine(error_path, val.Name));
                    //File.WriteAllText(Path.GetDirectoryName(val.FullName) + "\\error\\" + val.Name + ".error", email_result);
                    MessageBox.Show("Ошибка поиска TradeConfirmation! Подсунули не тот файл... он перемещён в папку error\r\nОбязательно проверить! Не запускать снова!");
                    label_status.ForeColor = Color.Red;
                    label_status.Text = "Error";
                    return;
                }

                //r = null;

                //Regex r = new Regex(@"(https\:\/\/steamcommunity\.com\/tradeoffer\/[0-9]+\/confirm\?accountid\=[a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)");
                r = new Regex("(https\\:\\/\\/steamcommunity\\.com\\/tradeoffer\\/[0-9]+\\/confirm\\?accountid\\=[^>' \"]+)");
                m = r.Match(email_html);
                if (m.Success)
                {
                    bool confirmed = false;
                    int try_count = 0;
                    //MessageBox.Show(m.Groups[1].ToString());
                    while (!confirmed)
                    {
                        try_count++;
                        if(try_count > 5)
                        {
                            File.Move(val.FullName, Path.Combine(error_path, val.Name));
                            label_status.ForeColor = Color.Red;
                            label_status.Text = "Error";
                            MessageBox.Show("После 5-и попыток не удалось подтвердить обмен!\r\nФайл перемещён в папку error\r\nОбязательно проверить! Не запускать снова!");
                            return;
                        }
                        webBrowser1.Navigate(new Uri(m.Groups[1].ToString()));
                        while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
                        {
                            Application.DoEvents();
                        }
                        //MessageBox.Show(webBrowser1.Url.ToString());
                        string email_result = webBrowser1.Document.Body.InnerHtml;
                        //MessageBox.Show(html);
                        if (email_result.ToLower().IndexOf("<h2>обмен подтвержден</h2>", StringComparison.Ordinal) > 0 || email_result.ToLower().IndexOf("<h2>trade confirmed</h2>", StringComparison.Ordinal) > 0)
                        {
                            //File.WriteAllText(val.FullName + ".done", email_result);
                            File.Delete(val.FullName);
                            email_counter++;
                            label_email_counter.Text = "Activating eMails: " + email_counter.ToString() + " / " + emails.Expand().Count();
                            break;
                        }
                        /*else
                        {
                            File.Move(val.FullName, Path.GetDirectoryName(val.FullName) + "\\error\\" + val.Name);
                            //File.WriteAllText(Path.GetDirectoryName(val.FullName) + "\\error\\" + val.Name + ".error", email_result);
                            //MessageBox.Show("Ошибка проверки подтверждения! Сорей всего не тот язык... файл перемещён в папку error\r\nОбязательно проверить! Не запускать снова!");
                            label_status.ForeColor = Color.Red;
                            label_status.Text = "Error";
                            return;
                        }*/
                        //webBrowser1.Dispose();
                    }

                    continue;
                }
                else
                {
                    File.Move(val.FullName, Path.Combine(error_path, val.Name));
                    //File.WriteAllText(Path.GetDirectoryName(val.FullName) + "\\error\\" + val.Name + ".error", email_result);
                    //MessageBox.Show("Ошибка поиска tradeoffer! Сорей всего тупит стим... файл перемещён в папку error\r\nОбязательно проверить! Не запускать снова!");
                    label_status.ForeColor = Color.Red;
                    label_status.Text = "Error";
                    continue;
                }
            }
            button1.Enabled = true;
            label_status.ForeColor = Color.Green;
            label_status.Text = "Done";
        }
    }
}
