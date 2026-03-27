using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using System.IO;

namespace GenieSupervisor
{
	/// <summary>
	/// Interaction logic for WindowUserLogin.xaml
	/// </summary>
	public partial class WindowUserLogin : Window
	{
		MainWindow _app;
		string UserAccount;
		string UserPassWord;
		string UserName;

		public WindowUserLogin(MainWindow app)
		{
			InitializeComponent();
			_app = app;
			this.KeyDown += WindowUserLogin_KeyDown;
		}

		private void WindowUserLogin_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if(e.Key == Key.Enter) {
				LoginMouseLeftButtonDown(null, null);
			}
			if(e.Key == Key.System && e.SystemKey == Key.F4) {
				e.Handled = true;
				this.Close();
			}
			if(e.Key == Key.Escape) {
				_app.bIsLoginSuccess = false;
				this.Close();
			}
		}

		private void LoginMouseLeftButtonDown(object sender, RoutedEventArgs e)
		{
			UserAccount = tbUserAccount.Text.Trim();
			UserPassWord = tbPassWord.Password.ToString().Trim();
			_app.bIsLoginSuccess = LoadAuthenticateFileandVerifyLogin();

			if(_app.bIsLoginSuccess)
            {
				_app.UserName = UserName;
				this.Close();
			}
		}

        private bool LoadAuthenticateFileandVerifyLogin()
        {
			string strbinFilePath = @"C:\Users\Public\GenieAuthenticator";
			if (!Directory.Exists(strbinFilePath))
				Directory.CreateDirectory(strbinFilePath);

			string[] binFile = Directory.GetFiles(strbinFilePath, "*.bin");
			if (binFile == null || binFile.Length == 0)
            {
				tbUncorrectLogin.Text = "This system has not been registered. Please register below to log in to the application";
				tbUncorrectLogin.Visibility = Visibility.Visible;
				return false;
			}

			string userAccount;
			string userPassword;
			string accessLevel;
			string macAddress;
			DateTime registerDt;
            try
            {
                using (Stream stream = File.Open(binFile[0], FileMode.Open))
                {
                    var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    userAccount = (string)bformatter.Deserialize(stream);
                    userPassword = (string)bformatter.Deserialize(stream);
					UserName = (string)bformatter.Deserialize(stream);
                    accessLevel = (string)bformatter.Deserialize(stream);
                    macAddress = (string)bformatter.Deserialize(stream);
                    registerDt = DateTime.Parse((string)bformatter.Deserialize(stream));
                }

				if (userAccount != UserAccount || userPassword != UserPassWord)
				{
					tbUncorrectLogin.Text = "UserName or password you entered are not valid. Please try again with valid entry";
					tbUncorrectLogin.Visibility = Visibility.Visible;
					return false;
				}

				DateTime todayDay = DateTime.Now.Date;
				double dateDiff = (todayDay - registerDt.Date).TotalDays;
				if (dateDiff > 30)
				{
					tbUncorrectLogin.Text = "This Account Registration has been exceeded 30 days. Please register again below to log in to the application ";
					tbUncorrectLogin.Visibility = Visibility.Visible;
					return false;
				}

				string MacAddress = Utilities.GetMacAddress();
				Utilities.LogMessage("This PC MAC : " + MacAddress);
				if (MacAddress != macAddress)
				{
					Utilities.LogMessage("MacAdress not matching : This PC MAC : " + MacAddress + " and Register MAC : " + macAddress);
					tbUncorrectLogin.Text = "This system has not been registered. Please register below to log in to the application";
					tbUncorrectLogin.Visibility = Visibility.Visible;
					return false;
				}

				return true;
			}
			catch (Exception ex)
            {
				tbUncorrectLogin.Text = "Failed to Read Account information or Account info file has been corrupted";
				tbUncorrectLogin.Visibility = Visibility.Visible;
				Utilities.LogMessage("Exception while reading Authenticator bin file : " + ex.Message);
				return false;
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_app.Activate();
            base.OnClosing(e);
		}

		private void ButtonClose_Click(object sender, MouseButtonEventArgs e)
		{
			this.Close();
		}

        private void ButtonRegister_Click(object sender, MouseButtonEventArgs e)
        {
			this.Close(); 
			WindowUserSignUp userSignUp = new WindowUserSignUp(_app);
			userSignUp.ShowDialog();			
		}
    }
}
