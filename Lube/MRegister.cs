using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lube
{
    public partial class MRegister : Form
    {
        public MRegister()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
            Login login = new Login();
            login.Show();
           // this.Hide();
        }
        #region 判断是否为有效输入

        private bool IsValidataInput()
        {
            if (txtLoginNo.Text.Trim() == "")
            {
                MessageBox.Show("请输入账号！", "注册提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtLoginNo.Focus();
                return false;
            }

            else if (txtLoginPwd.Text == "")
            {
                MessageBox.Show("请输入密码！", "注册提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtLoginPwd.Focus();
                return false;
            }

            else if (DtxtLoginPwd.Text == "")
            {
                MessageBox.Show("请再次确认输入密码！", "注册提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DtxtLoginPwd.Focus();
                return false;
            }

            else if (!txtLoginPwd.Text.Equals(DtxtLoginPwd.Text))
            {
                MessageBox.Show("两次输入的密码不一致，请重新输入！", "注册提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DtxtLoginPwd.Clear();
                txtLoginPwd.Clear();

                txtLoginPwd.Focus();
                return false;
            }

            return true;
        }

        #endregion

        private bool IsValidataUser(string loginNo, string loginPwd,ref string message)
        {
            string sql = String.Format("select count(*) from DB_ManageInfo where loginNo = '{0}'", loginNo);

            try
            {
                int result = Lube.SqliteHelper.ExecuteScalar(sql);
                if (result == 1)
                {
                    message = "该账号已经存在，请重新注册！";
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch { }
            finally
            {
                SQLiteConnection conn = Lube.SqliteHelper.GetConnection();
                conn.Close();
            }
            return false;

        }

        private void btnEnter_Click(object sender, EventArgs e)
        {
            bool isValidUser = false;
            string message = "";

            if (IsValidataInput())
            {
                //验证用户是否为合法用户
                isValidUser = IsValidataUser(txtLoginNo.Text.Trim(), txtLoginPwd.Text,ref message);

                if (isValidUser)
                {
                    string sql = String.Format("insert into DB_ManageInfo(loginNo,loginPwd) values ('{0}','{1}')", txtLoginNo.Text.Trim(), txtLoginPwd.Text.Trim());

                    int result = Lube.SqliteHelper.ExecuteSql(sql);

                    if (result > 0)
                    {
                        MessageBox.Show("注册成功！", "注册提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                        this.Close();
                        Login login = new Login();
                        login.Show();
                    }
                    else
                    {
                        MessageBox.Show("注册失败！", "注册提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    }
                }
                else
                {
                    MessageBox.Show(message, "注册提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }

                txtLoginNo.Clear();
                txtLoginPwd.Clear();
                DtxtLoginPwd.Clear();
                
                txtLoginNo.Focus();
            }
        }

        private void MRegister_Load(object sender, EventArgs e)
        {
            txtLoginNo.Focus();
        }
    }
}
