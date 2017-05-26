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
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        // 验证用户的输入,成功返回true,失败返回false
        private bool IsValidataInput()
        {
            if (txtLoginNo.Text.Trim() == "")
            {
                MessageBox.Show("请输入账号！", "登陆提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtLoginNo.Focus();
                return false;
            }
            else if (txtLoginPwd.Text == "")
            {
                MessageBox.Show("请输入密码！", "登陆提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtLoginPwd.Focus();
                return false;
            }
            return true;
        }

        //传递用户账号、密码,合法返回true,不合法返回false
        //message参数用来记录验证失败的原因
        private bool IsValidataUser(string loginNo, string loginPwd, ref string message)
        {
            string sql = String.Format("select count(*) from DB_ManageInfo where loginNo = '{0}' and loginPwd = '{1}'", loginNo, loginPwd);
            try
            {
                int result = SqliteHelper.ExecuteScalar(sql);
                if (result == 0)
                {
                    message = "该用户名或密码不存在！";
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
                SQLiteConnection conn = SqliteHelper.GetConnection();
                conn.Close();
            }
            return false;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            //标识是否为合法用户
            bool isValidUser = false;
            string message = "";

            if (IsValidataInput())
            {
                //验证用户是否为合法用户
                isValidUser = IsValidataUser(txtLoginNo.Text.Trim(), txtLoginPwd.Text, ref message);

                if (isValidUser)
                {
                    //记录登陆用户名和用户类型
                    UserHelper.loginName = txtLoginNo.Text.Trim();
                    pqRecord sfm = new pqRecord();
                    sfm.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show(message, "登陆提示", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
            }

        }

        private void btnRes_Click(object sender, EventArgs e)
        {
            MRegister reg = new MRegister();
            reg.Show();
            this.Hide();
        }
    }
}
