using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt;
using System.Net;
using System.Net.Mail;
using System.Timers;

namespace GenieSupervisor
{
	/// <summary>
	/// Interaction logic for WindowUserSignUp.xaml
	/// </summary>
	public partial class WindowUserSignUp : Window, INotifyPropertyChanged
	{
		string userName = "";
		public string UserName
		{
			get {
				return userName;
			}
			set {
				userName = value;
				OnPropertyChanged("UserName");
			}
		}

		int accessLevel = 0;

		public List<string> AccessLevelCategory = new List<string>() { "Admin", "Manager", "Guest" };


		public string AccessLevel
		{
			get { return AccessLevelCategory[accessLevel]; }
			set { accessLevel = AccessLevelCategory.IndexOf(value); }
		}

		public Brush AccessLevelBrushColor
		{
			get {
				if(accessLevel == 0) {
					return Brushes.LightGreen;
				}
				else if(accessLevel == 1) {
					return Brushes.DeepSkyBlue;
				}
				else {
					return Brushes.LightYellow;
				}
			}
		}

		string userAccount = "";
		public string UserAccount
		{
			get {
				return userAccount;
			}
			set {
				userAccount = value;
				OnPropertyChanged("UserAccount");
			}
		}

		string sourceImageUser = "";

		public string UserPassword;

		public int UserIndex = 0;
		public bool isAddNewUser = true;

		MainWindow _app;
		MqttClient mqttClient;
		string MacAddress;

		public WindowUserSignUp(MainWindow app)
		{
			InitializeComponent();
			_app = app;

			this.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(HandleEsc);
			DataContext = this;
		}

		private void HandleEsc(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if(e.Key == Key.Escape)
				this.Close();
		}

        protected override void OnClosing(CancelEventArgs e)
        {
			if (mqttClient != null && mqttClient.IsConnected)
				mqttClient.Disconnect();
		}

        protected override void OnClosed(EventArgs e)
		{
			WindowUserLogin userLogin = new WindowUserLogin(_app);
			userLogin.ShowDialog();
		}

		private async void ButtonSave_Click(object sender, MouseButtonEventArgs e)
		{
			tbUnvalidPassword.Visibility = Visibility.Collapsed;
			tbUnvalidAccount.Visibility = Visibility.Collapsed;
			tbInvalidEntry.Visibility = Visibility.Collapsed;
			tbResponseMsg.Visibility = Visibility.Collapsed;
			if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(UserAccount) || string.IsNullOrWhiteSpace(tbPassWord.Password) || string.IsNullOrWhiteSpace(tbConfirmPassWord.Password))
			{
				tbInvalidEntry.Visibility = Visibility.Visible;
				return;
			}

			if(tbConfirmPassWord.Password != tbPassWord.Password) {
				tbUnvalidPassword.Visibility = Visibility.Visible;
				return;
			}

			UserPassword = tbPassWord.Password;
			bool isRegisted = await RegisterUserLogin();
			//this.Close();
		}

		Timer TimerResponseWait = new Timer();
		private async Task<bool> RegisterUserLogin()
		{
			busyIndicator.IsBusy = true;
			string subToken = Utilities.GenerateToken(25).ToUpper();
			bool isSubscribe = await SubscribeMqtt(subToken);
			if (isSubscribe)
            {
				if (ConfigureandSendEMail(subToken))
                {
					TimerResponseWait = new Timer()
					{
						AutoReset = true
					};
					TimerResponseWait.Elapsed += new ElapsedEventHandler(ExpireResonseWait);
					TimerResponseWait.Interval = 1000 * 60 * 10; // wait for 10 minutes
					TimerResponseWait.Start();
					return true;
				}
				else
				{
					busyIndicator.IsBusy = false;
					MessageBox.Show("Failed to send the request! Please check your Internet Connection.", "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return false;
				}
			}
			else
            {
				busyIndicator.IsBusy = false;
				MessageBox.Show("Not able to Establish Network Connection!\nPlease check your Internet Connection.", "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
            }
			return true;
		}

        private void ExpireResonseWait(object sender, ElapsedEventArgs e)
        {
			Dispatcher.Invoke(() =>
			{
				busyIndicator.IsBusy = false;
				TimerResponseWait.Stop();
				tbResponseMsg.Text = "Request timeout has expired. No Response recieved from Admin";
				tbResponseMsg.Visibility = Visibility.Visible;
				tbResponseMsg.Foreground = Brushes.Red;

				if (mqttClient.IsConnected)
					mqttClient.Disconnect();
			});
		}

        private bool ConfigureandSendEMail(string strSubToken)
        {
			// Set up email details
			string senderEmail = "santu.gol@gmail.com";
			string senderPassword = "fcmkssmxxasnhknr";
			string recieverEmail = "santu.gol@gmail.com";   //"bhabani.shankarm@emagegroup.com";		//"santu.gol@gmail.com";
			string recieverEmail2 = "bhabani.shankarm@emagegroup.com";
			MacAddress = Utilities.GetMacAddress();

			string body = $"Hello,<h4>GenieSupervisor Registration Request</h4>" +
						"<div>User Name : " + UserName + "</div>" +
						"<div>Access Level : " + AccessLevel + "</div>" +
						"<div>MAC Address : " + MacAddress + "</div>" +
						"<div>Registration Date : " + DateTime.Now.ToString() + "</div>" +
						"<h4>Token : " + strSubToken  + "</h4>";

			string linkUrl = "http://localhost:8088/ConfirmationPage";
			string linkText = "link";

			// Create the email message
			MailMessage mailMeassage = new MailMessage();
			mailMeassage.From = new MailAddress(senderEmail);
			mailMeassage.To.Add(recieverEmail);
			//mailMeassage.To.Add(recieverEmail2);
			mailMeassage.Subject = "Request Confirmation";
			string htmlBody = body + $"Please copy above token and confirm or decline request using this <a href=\"{linkUrl}\" Font-Italic=\"true\">{linkText}</a>.";

			mailMeassage.Body = htmlBody;
			mailMeassage.IsBodyHtml = true;

			SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587)
			{
				EnableSsl = true,
				UseDefaultCredentials = false,
				Credentials = new NetworkCredential(senderEmail, senderPassword)
			};

			try
			{
				// Send the email
				smtpClient.Send(mailMeassage);
			}
			catch (Exception ex)
			{
				Utilities.LogMessage("An error occurred while sending the email: " + ex.Message);
				return false;
			}

			return true;
		}

		public async Task<bool> SubscribeMqtt(string strSubToken)
		{
			bool bIsOK = false;
			await Task.Run(() =>
			{
				try
				{
                    mqttClient = new MqttClient("test.mosquitto.org", 1883, false, null, null, MqttSslProtocols.None);
                    //mqttClient = new MqttClient("broker.hivemq.com", 1883, false, null, null, MqttSslProtocols.None);
					mqttClient.Subscribe(new string[] { strSubToken },
							new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
					
					mqttClient.Connect("MQTT_Client");
					mqttClient.MqttMsgPublishReceived += MqttClient_MqttMsgPublishReceived;
					bIsOK = true;
				}
                catch(Exception ex)
                {
					bIsOK = false;
                }
			});

			return bIsOK ? true : false;
		}

		private void MqttClient_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
		{
			var message = Encoding.UTF8.GetString(e.Message);
			Dispatcher.Invoke(() =>
			{
				busyIndicator.IsBusy = false;
				if (message == "Confirm")
				{
					tbResponseMsg.Text = "Request has been Confirmed. Click on Back and Login using Username and Password\nThis Registration will be valid for 30 days.";
					tbResponseMsg.Visibility = Visibility.Visible;
					tbResponseMsg.Foreground = Brushes.LawnGreen;

					GenerateUserAccessBinFile();
				}
				else
				{
					tbResponseMsg.Text = "Request has been Declined! Please contact your manager for authorization";
					tbResponseMsg.Visibility = Visibility.Visible;
					tbResponseMsg.Foreground = Brushes.Red;
				}

				tbAccount.IsEnabled = false;
				tbPassWord.IsEnabled = false;
				tbConfirmPassWord.IsEnabled = false;
				tbUserName.IsEnabled = false;
				cbAccessLevel.IsEnabled = false;
				btnSave.IsEnabled = false;
			});

			if (mqttClient != null && mqttClient.IsConnected)
				mqttClient.Disconnect();
		}

        private void GenerateUserAccessBinFile()
        {
			string binSavePath = @"C:\Users\Public\GenieAuthenticator";
			if (!Directory.Exists(binSavePath))
				Directory.CreateDirectory(binSavePath);

			string serializationFile = Path.Combine(binSavePath, "authenticate.bin");
			using (MemoryStream stream = new MemoryStream())
			{
				var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				bformatter.Serialize(stream, UserAccount);
				bformatter.Serialize(stream, UserPassword);
				bformatter.Serialize(stream, UserName);
				bformatter.Serialize(stream, AccessLevel);
				bformatter.Serialize(stream, MacAddress);
				bformatter.Serialize(stream, DateTime.Now.ToString());

				foreach (string file in Directory.GetFiles(binSavePath, "*.bin"))
					File.Delete(file);

				//Save to new file
				Stream FileStream = File.Open(serializationFile, FileMode.Create);
				stream.WriteTo(FileStream);
				FileStream.Close();
			}			
		}

        private void ButtonClose_Click(object sender, MouseButtonEventArgs e)
		{
			this.Close();
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string name)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if(handler != null)
				handler(this, new PropertyChangedEventArgs(name));
		}

		private void cbAccessLevelSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if(cbAccessLevel.SelectedValue == null)
				return;
			Telerik.Windows.Controls.RadComboBoxItem typeItem = (Telerik.Windows.Controls.RadComboBoxItem)cbAccessLevel.SelectedItem;
			if(typeItem.Content == null)
				return;
			string value = typeItem.Content.ToString();
			if(value != AccessLevel) {
				AccessLevel = value;
				OnPropertyChanged("AccessLevelBrushColor");
			}
		}

		private void ChangePhotoMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
			fileDialog.Title = "Choose User Picture";
			fileDialog.Filter = "Image Files (*.bmp;*.jpg;*.png)|*.bmp;*.jpg;*.png|All files (*.*)|*.*";
			if(fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				string fileName = fileDialog.FileName;
				System.Threading.Thread.Sleep(100);
				sourceImageUser = fileName;
				//UserImage.ImageSource = UserManager.SetImage(sourceImageUser);
			}
		}
	}
}
