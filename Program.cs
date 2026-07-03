using ParkingSystemWithDB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SimpleParkingSystem
{
    // 1. ABSTRACTION - Abstract class
    public abstract class Vehicle
    {
        private string _plateNumber;
        private string _ownerName;
        private DateTime _entryTime;

        public string PlateNumber
        {
            get { return _plateNumber; }
            set { _plateNumber = value; }
        }

        public string OwnerName
        {
            get { return _ownerName; }
            set { _ownerName = value; }
        }

        public DateTime EntryTime
        {
            get { return _entryTime; }
            set { _entryTime = value; }
        }

        public abstract decimal CalculateFee(int hours);
        public abstract string GetVehicleType();
    }

    // 2. INHERITANCE + POLYMORPHISM
    public class Car : Vehicle
    {
        public override decimal CalculateFee(int hours)
        {
            decimal fee = hours * 50;
            if (hours > 8) fee = fee * 0.90m;
            return fee;
        }
        public override string GetVehicleType() => "Car";
    }

    public class Bike : Vehicle
    {
        public override decimal CalculateFee(int hours)
        {
            decimal fee = hours * 20;
            if (hours > 8) fee = fee * 0.90m;
            return fee;
        }
        public override string GetVehicleType() => "Bike";
    }

    public class Truck : Vehicle
    {
        public override decimal CalculateFee(int hours)
        {
            decimal fee = hours * 100;
            if (hours > 8) fee = fee * 0.90m;
            return fee;
        }
        public override string GetVehicleType() => "Truck";
    }

    // 3. USER CLASS - Encapsulation
    public class User
    {
        private int _userID;
        private string _fullName;
        private string _email;
        private string _phone;
        private string _cnic;
        private string _username;
        private string _password;
        private decimal _walletBalance;

        public int UserID
        {
            get { return _userID; }
            set { _userID = value; }
        }

        public string FullName
        {
            get { return _fullName; }
            set { _fullName = value; }
        }

        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        public string Phone
        {
            get { return _phone; }
            set { _phone = value; }
        }

        public string Cnic
        {
            get { return _cnic; }
            set { _cnic = value; }
        }

        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        public decimal WalletBalance
        {
            get { return _walletBalance; }
            set { _walletBalance = value; }
        }

        public User(string fullName, string email, string phone, string cnic, string username, string password)
        {
            FullName = fullName;
            Email = email;
            Phone = phone;
            Cnic = cnic;
            Username = username;
            Password = password;
            WalletBalance = 0;
        }
    }

    // 4. PARKING SLOT CLASS (Enhanced)
    public class ParkingSlot
    {
        private string _slotNumber;
        private int _floorNo;
        private bool _isPremium;
        private bool _isAvailable;
        private Vehicle _parkedVehicle;
        

        public string SlotNumber
        {
            get { return _slotNumber; }
            set { _slotNumber = value; }
        }

        public int FloorNo
        {
            get { return _floorNo; }
            set { _floorNo = value; }
        }

        public bool IsPremium
        {
            get { return _isPremium; }
            set { _isPremium = value; }
        }

        public bool IsAvailable
        {
            get { return _isAvailable; }
            set { _isAvailable = value; }
        }

        public Vehicle ParkedVehicle
        {
            get { return _parkedVehicle; }
            set { _parkedVehicle = value; }
        }

        public string SlotType
        {
            get { return _isPremium ? "Premium (+20%)" : "Regular"; }
        }

        public decimal PremiumPercent
        {
            get { return _isPremium ? 20 : 0; }
        }
      

        public ParkingSlot(string slotNumber, int floorNo, bool isPremium = false)
        {
            _slotNumber = slotNumber;
            _floorNo = floorNo;
            _isPremium = isPremium;
            _isAvailable = true;
            _parkedVehicle = null;
        }
    }

    // 5. PARKING MANAGER (Database Integrated)
    public class ParkingManager
    {
        private static ParkingManager _instance;
        private List<User> _users;
        private User _currentUser;

        public static ParkingManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ParkingManager();
                return _instance;
            }
        }

        private ParkingManager()
        {
            _users = new List<User>();
            LoadUsersFromDatabase();
        }

        //  LOAD USERS FROM DATABASE 
        private void LoadUsersFromDatabase()
        {
            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery("sp_GetAllUsers");
                _users.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    User user = new User(
                        row["FullName"].ToString(),
                        row["Email"].ToString(),
                        row["Phone"].ToString(),
                        row["CNIC"].ToString(),
                        row["Username"].ToString(),
                        row["UserPassword"].ToString()
                    );
                    user.UserID = Convert.ToInt32(row["UserID"]);
                    user.WalletBalance = Convert.ToDecimal(row["WalletBalance"]);
                    _users.Add(user);
                }
            }
            catch { }
        }

        //  USER REGISTRATION (SAVES TO DATABASE) 
        public bool RegisterUser(string fullName, string email, string phone, string cnic, string username, string password)
        {
            try
            {
                SqlParameter[] parameters = {
                new SqlParameter("@FullName", fullName),
                new SqlParameter("@Email", email),
                new SqlParameter("@Phone", phone),
                new SqlParameter("@CNIC", cnic),
                new SqlParameter("@Username", username),
                new SqlParameter("@Password", password)
            };

                DataTable dt = DatabaseHelper.ExecuteQuery("sp_RegisterUser", parameters);
                if (dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["Result"]) == 1)
                {
                    LoadUsersFromDatabase(); // Refresh list
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        // USER LOGIN (CHECKS DATABASE) 
        public bool LoginUser(string username, string password)
        {
            try
            {
                SqlParameter[] parameters = {
            new SqlParameter("@Username", username),
            new SqlParameter("@Password", password)
        };

                DataTable dt = DatabaseHelper.ExecuteQuery("sp_LoginUser", parameters);

                // Debug message 
                MessageBox.Show("Rows returned: " + dt.Rows.Count);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];

                    // Debug – show what we got
                    MessageBox.Show("UserID: " + row["UserID"] + "\nFullName: " + row["FullName"]);

                    _currentUser = new User(
                        row["FullName"].ToString(),
                        row["Email"].ToString(),
                        "", "", "", ""
                    );
                    _currentUser.UserID = Convert.ToInt32(row["UserID"]);
                    _currentUser.Username = row["Username"].ToString();
                    _currentUser.WalletBalance = Convert.ToDecimal(row["WalletBalance"]);
                    return true;
                }
                else
                {
                    MessageBox.Show("No rows returned. Check stored procedure or connection.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR in LoginUser: " + ex.Message + "\n\n" + ex.StackTrace);
                return false;
            }
        }

        // ADMIN LOGIN 
        public bool AdminLogin(string username, string password)
        {
            try
            {
                SqlParameter[] parameters = {
                new SqlParameter("@Username", username),
                new SqlParameter("@Password", password)
            };
                DataTable dt = DatabaseHelper.ExecuteQuery("sp_AdminLogin", parameters);
                return dt.Rows.Count > 0;
            }
            catch
            {
                return (username == "admin" && password == "admin123");
            }
        }

        public User GetCurrentUser() { return _currentUser; }
        public List<User> GetAllUsers() { LoadUsersFromDatabase(); return _users; }

        //  PARK VEHICLE (SAVES TO DATABASE) 
        public bool ParkVehicle(string plateNumber, string vehicleType, string ownerName)
        {
            try
            {
                SqlParameter[] parameters = {
                new SqlParameter("@PlateNumber", plateNumber),
                new SqlParameter("@VehicleTypeName", vehicleType),
                new SqlParameter("@OwnerName", ownerName),
                new SqlParameter("@OwnerCNIC", _currentUser?.Cnic ?? "GUEST"),
                new SqlParameter("@Username", _currentUser?.Username)
            };

                DataTable dt = DatabaseHelper.ExecuteQuery("sp_ParkVehicle", parameters);
                return dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["Result"]) == 1;
            }
            catch
            {
                return false;
            }
        }

        //  EXIT VEHICLE (SAVES TO DATABASE) 
        public decimal ExitVehicle(string slotNumber, string paymentMethod = "Cash")
        {
            try
            {
                SqlParameter[] parameters = {
                new SqlParameter("@SlotNumber", slotNumber),
                new SqlParameter("@PaymentMethod", paymentMethod)
            };

                DataTable dt = DatabaseHelper.ExecuteQuery("sp_ExitVehicle", parameters);
                if (dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["Result"]) == 1)
                {
                    return Convert.ToDecimal(dt.Rows[0]["Amount"]);
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        //  GET TOTAL REVENUE FROM DATABASE 
        public decimal GetTotalRevenue()
        {
            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery("sp_GetTotalRevenue");
                if (dt.Rows.Count > 0)
                    return Convert.ToDecimal(dt.Rows[0]["TotalRevenue"]);
                return 0;
            }
            catch { return 0; }
        }

        //  GET ACTIVE VEHICLES COUNT 
        public int GetActiveVehiclesCount()
        {
            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery("sp_GetDashboardStats");
                if (dt.Rows.Count > 0)
                    return Convert.ToInt32(dt.Rows[0]["ActiveVehicles"]);
                return 0;
            }
            catch { return 0; }
        }

        //  GET ALL SLOTS (FOR DISPLAY) 
        public List<ParkingSlot> GetAllSlots()
        {
            List<ParkingSlot> slots = new List<ParkingSlot>();
            for (int i = 1; i <= 12; i++)
            {
                string slotNum = "S" + i.ToString("00");
                int floorNo = (i - 1) / 4 + 1;
                bool isPremium = (slotNum == "S03" || slotNum == "S07" || slotNum == "S11");
                slots.Add(new ParkingSlot(slotNum, floorNo, isPremium));
            }

            // Update availability from database
            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery("sp_GetActiveVehicles");
                foreach (DataRow row in dt.Rows)
                {
                    string slotNum = row["SlotNumber"].ToString();
                    var slot = slots.FirstOrDefault(s => s.SlotNumber == slotNum);
                    if (slot != null)
                    {
                        slot.IsAvailable = false;

                        // Build Vehicle object so Admin dashboard can show it  
                        string vType = row["VehicleType"].ToString();
                        Vehicle v = vType == "Bike" ? (Vehicle)new Bike()
                                  : vType == "Truck" ? (Vehicle)new Truck()
                                  : (Vehicle)new Car();

                        v.PlateNumber = row["PlateNumber"].ToString();
                        v.OwnerName = row["OwnerName"].ToString();
                        if (row.Table.Columns.Contains("EntryTime") && row["EntryTime"] != DBNull.Value)
                            v.EntryTime = Convert.ToDateTime(row["EntryTime"]);

                        slot.ParkedVehicle = v;
                    }
                }
            }
            catch { }

            return slots;
        }

        internal IEnumerable<object> GetAvailableSlots()
        {
            throw new NotImplementedException();
        }
    }

    // 6. IREPORTABLE INTERFACE
    public interface IReportable
    {
        void GenerateReport();
    }

    public class Admin : IReportable
    {
        public void GenerateReport()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = path + "\\ParkingReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";

            using (System.IO.StreamWriter w = new System.IO.StreamWriter(filePath))
            {
                w.WriteLine("===== PARKING REPORT =====");
                w.WriteLine("Date: " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss"));
                w.WriteLine("Total Revenue: Rs. " + ParkingManager.Instance.GetTotalRevenue());
                w.WriteLine("Active Vehicles: " + ParkingManager.Instance.GetActiveVehiclesCount());
                w.WriteLine("Registered Users: " + ParkingManager.Instance.GetAllUsers().Count);
                w.WriteLine("==========================");
            }

            MessageBox.Show("Report saved on Desktop!\n" + filePath, "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // 7. LOGIN FORM
    public class LoginForm : Form
    {
        private TextBox txtUsername, txtPassword;
        private Button btnUserLogin, btnAdminLogin, btnRegister;
        private Label lblError;

        public LoginForm()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "Parking System - Login";
            this.Size = new Size(450, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(28, 40, 70);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            Label lblTitle = new Label()
            {
                Text = "PARKING SYSTEM",
                Font = new Font("Arial", 22, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(100, 30),
                Size = new Size(250, 45),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblSub = new Label()
            {
                Text = "Login to Continue",
                Font = new Font("Arial", 12),
                ForeColor = Color.LightBlue,
                Location = new Point(130, 80),
                Size = new Size(190, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblUser = new Label()
            {
                Text = "Username:",
                Font = new Font("Arial", 11),
                ForeColor = Color.White,
                Location = new Point(60, 140),
                Size = new Size(100, 25)
            };

            txtUsername = new TextBox()
            {
                Location = new Point(170, 140),
                Size = new Size(200, 25),
                BackColor = Color.White
            };

            Label lblPass = new Label()
            {
                Text = "Password:",
                Font = new Font("Arial", 11),
                ForeColor = Color.White,
                Location = new Point(60, 180),
                Size = new Size(100, 25)
            };

            txtPassword = new TextBox()
            {
                Location = new Point(170, 180),
                Size = new Size(200, 25),
                BackColor = Color.White,
                PasswordChar = '*'
            };

            btnUserLogin = new Button()
            {
                Text = "Login as User",
                Location = new Point(60, 240),
                Size = new Size(150, 40),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 11, FontStyle.Bold)
            };
            btnUserLogin.Click += BtnUserLogin_Click;

            btnAdminLogin = new Button()
            {
                Text = "Login as Admin",
                Location = new Point(230, 240),
                Size = new Size(150, 40),
                BackColor = Color.FromArgb(200, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 11, FontStyle.Bold)
            };
            btnAdminLogin.Click += BtnAdminLogin_Click;

            btnRegister = new Button()
            {
                Text = "New User? Register Here",
                Location = new Point(120, 310),
                Size = new Size(200, 35),
                BackColor = Color.FromArgb(28, 40, 70),
                ForeColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10)
            };
            btnRegister.Click += BtnRegister_Click;

            lblError = new Label()
            {
                Text = "",
                Font = new Font("Arial", 9),
                ForeColor = Color.Red,
                Location = new Point(60, 360),
                Size = new Size(320, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.Controls.AddRange(new Control[] { lblTitle, lblSub, lblUser, txtUsername,
                                                   lblPass, txtPassword, btnUserLogin,
                                                   btnAdminLogin, btnRegister, lblError });
        }

        private void BtnUserLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUsername.Text) || string.IsNullOrEmpty(txtPassword.Text))
            {
                lblError.Text = "Please enter username and password!";
                return;
            }

            if (ParkingManager.Instance.LoginUser(txtUsername.Text, txtPassword.Text))
            {
                UserDashboard userForm = new UserDashboard();
                userForm.Show();
                this.Hide();
            }
            else
            {
                lblError.Text = "Invalid username or password!";
            }
        }

        private void BtnAdminLogin_Click(object sender, EventArgs e)
        {
            if (ParkingManager.Instance.AdminLogin(txtUsername.Text, txtPassword.Text))
            {
                AdminDashboard adminForm = new AdminDashboard();
                adminForm.Show();
                this.Hide();
            }
            else
            {
                lblError.Text = "Invalid admin credentials!\nUse: admin / admin123";
            }
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            RegisterForm regForm = new RegisterForm();
            regForm.ShowDialog();
        }
    }

    // 8. REGISTER FORM
    public class RegisterForm : Form
    {
        private TextBox txtFullName, txtEmail, txtPhone, txtCnic, txtUsername, txtPassword, txtConfirmPassword;

        public RegisterForm()
        {
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "Register New Account";
            this.Size = new Size(480, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(28, 40, 70);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            Label lblTitle = new Label()
            {
                Text = "CREATE ACCOUNT",
                Font = new Font("Arial", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(120, 20),
                Size = new Size(250, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            int y = 80;
            AddField("Full Name:", ref txtFullName, y);
            AddField("Email:", ref txtEmail, y + 50);
            AddField("Phone:", ref txtPhone, y + 100);
            AddField("CNIC:", ref txtCnic, y + 150);
            AddField("Username:", ref txtUsername, y + 200);
            AddField("Password:", ref txtPassword, y + 250);
            AddField("Confirm Password:", ref txtConfirmPassword, y + 300);
            txtPassword.PasswordChar = '*';
            txtConfirmPassword.PasswordChar = '*';

            Button btnCreate = new Button()
            {
                Text = "CREATE ACCOUNT",
                Location = new Point(130, 460),
                Size = new Size(200, 45),
                BackColor = Color.FromArgb(0, 150, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 11, FontStyle.Bold)
            };
            btnCreate.Click += BtnCreate_Click;

            this.Controls.Add(lblTitle);
            this.Controls.Add(btnCreate);
        }

        private void AddField(string labelText, ref TextBox textBox, int y)
        {
            Label label = new Label()
            {
                Text = labelText,
                Font = new Font("Arial", 10),
                ForeColor = Color.White,
                Location = new Point(50, y),
                Size = new Size(120, 25)
            };

            textBox = new TextBox()
            {
                Location = new Point(180, y),
                Size = new Size(230, 25),
                BackColor = Color.White
            };

            this.Controls.Add(label);
            this.Controls.Add(textBox);
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFullName.Text) || string.IsNullOrEmpty(txtEmail.Text) ||
                string.IsNullOrEmpty(txtPhone.Text) || string.IsNullOrEmpty(txtCnic.Text) ||
                string.IsNullOrEmpty(txtUsername.Text) || string.IsNullOrEmpty(txtPassword.Text))
            {
                MessageBox.Show("Please fill all fields!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtPassword.Text != txtConfirmPassword.Text)
            {
                MessageBox.Show("Passwords do not match!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool success = ParkingManager.Instance.RegisterUser(
                txtFullName.Text, txtEmail.Text, txtPhone.Text, txtCnic.Text,
                txtUsername.Text, txtPassword.Text);

            if (success)
            {
                MessageBox.Show("Account created successfully!\nYou can now login.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            else
            {
                MessageBox.Show("Username already exists!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // 9. USER DASHBOARD (With Slot Selection Feature)
    public class UserDashboard : Form
    {
        private ComboBox cmbVehicleType;
        private ComboBox cmbSelectSlot;
        private TextBox txtPlateNumber;
        private Button btnPark, btnExit, btnLogout, btnRefreshSlots;
        private DataGridView dgvFreeSlots;
        private Label lblRevenue, lblUserInfo, lblWalletBalance;
        private ParkingManager manager;

        public UserDashboard()
        {
            manager = ParkingManager.Instance;
            SetupUI();
            RefreshFreeSlots();
            UpdateStats();
        }

        private void SetupUI()
        {
            this.Text = "User Dashboard - Parking System";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(28, 40, 70);

            // Header Panel
            Panel header = new Panel() { Location = new Point(0, 0), Size = new Size(1100, 80), BackColor = Color.FromArgb(15, 25, 50) };

            User currentUser = manager.GetCurrentUser();
            lblUserInfo = new Label()
            {
                Text = $"Welcome: {currentUser.FullName}\nEmail: {currentUser.Email}",
                Font = new Font("Arial", 10),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                Size = new Size(250, 50)
            };

            lblWalletBalance = new Label()
            {
                Text = $"Wallet: Rs. {currentUser.WalletBalance}",
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.LightGreen,
                Location = new Point(20, 55),
                Size = new Size(150, 25)
            };

            lblRevenue = new Label()
            {
                Text = "Total Revenue: Rs. 0",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.LightGreen,
                Location = new Point(700, 25),
                Size = new Size(200, 35),
                BackColor = Color.FromArgb(15, 25, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnLogout = new Button()
            {
                Text = "LOGOUT",
                Location = new Point(1000, 25),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(200, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLogout.Click += (s, e) => {
                LoginForm login = new LoginForm();
                login.Show();
                this.Close();
            };

            header.Controls.AddRange(new Control[] { lblUserInfo, lblWalletBalance, lblRevenue, btnLogout });

            // Main Panel
            Panel mainPanel = new Panel() { Location = new Point(10, 90), Size = new Size(1060, 560), BackColor = Color.FromArgb(28, 40, 70) };

            // Left Panel - User Info and Parking Form
            Panel leftPanel = new Panel() { Location = new Point(0, 0), Size = new Size(320, 560), BackColor = Color.FromArgb(28, 40, 70), BorderStyle = BorderStyle.FixedSingle };

            Label lblUserDetail = new Label()
            {
                Text = $"USER DETAILS\n\nName: {currentUser.FullName}\nPhone: {currentUser.Phone}\nCNIC: {currentUser.Cnic}",
                Font = new Font("Arial", 10),
                ForeColor = Color.LightBlue,
                Location = new Point(15, 20),
                Size = new Size(290, 100)
            };

            Label lblParkingForm = new Label()
            {
                Text = "PARK YOUR VEHICLE",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 140),
                Size = new Size(290, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(0, 120, 215)
            };

            Label lblPlate = new Label() { Text = "License Plate:", Location = new Point(15, 185), Size = new Size(100, 25), ForeColor = Color.White };
            txtPlateNumber = new TextBox() { Location = new Point(15, 215), Size = new Size(290, 25), BackColor = Color.White };

            Label lblType = new Label() { Text = "Vehicle Type:", Location = new Point(15, 255), Size = new Size(100, 25), ForeColor = Color.White };
            cmbVehicleType = new ComboBox() { Location = new Point(15, 285), Size = new Size(290, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbVehicleType.Items.AddRange(new string[] { "Car", "Bike", "Truck" });
            cmbVehicleType.SelectedIndex = 0;
            cmbVehicleType.SelectedIndexChanged += (s, e) => RefreshFreeSlots();

            Label lblSelectSlot = new Label() { Text = "Select Slot:", Location = new Point(15, 325), Size = new Size(100, 25), ForeColor = Color.White };
            cmbSelectSlot = new ComboBox() { Location = new Point(15, 355), Size = new Size(290, 30), DropDownStyle = ComboBoxStyle.DropDownList };

            btnPark = new Button()
            {
                Text = "PARK VEHICLE",
                Location = new Point(15, 405),
                Size = new Size(290, 45),
                BackColor = Color.FromArgb(0, 150, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 11, FontStyle.Bold)
            };
            btnPark.Click += BtnPark_Click;

            btnRefreshSlots = new Button()
            {
                Text = "REFRESH SLOTS",
                Location = new Point(15, 460),
                Size = new Size(290, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRefreshSlots.Click += (s, e) => RefreshFreeSlots();

            // Exit Section
            Panel exitPanel = new Panel() { Location = new Point(15, 500), Size = new Size(290, 50), BackColor = Color.FromArgb(15, 25, 50) };

            Label lblExitTitle = new Label()
            {
                Text = "EXIT VEHICLE - Enter Slot:",
                Font = new Font("Arial", 9, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(5, 15),
                Size = new Size(140, 25)
            };

            TextBox txtExitSlot = new TextBox() { Location = new Point(150, 13), Size = new Size(60, 25) };
            btnExit = new Button()
            {
                Text = "EXIT",
                Location = new Point(220, 10),
                Size = new Size(60, 30),
                BackColor = Color.FromArgb(200, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnExit.Click += (s, e) => {
                string slot = txtExitSlot.Text.Trim();
                if (!string.IsNullOrEmpty(slot))
                {
                    decimal fee = manager.ExitVehicle(slot.ToUpper(), "Cash");
                    if (fee > 0)
                    {
                        MessageBox.Show($"Vehicle Exited!\nTotal Fee: Rs. {fee}", "Bill", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshFreeSlots();
                        UpdateStats();
                    }
                    else
                    {
                        MessageBox.Show("Invalid or Empty Slot!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Please enter slot number!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            exitPanel.Controls.AddRange(new Control[] { lblExitTitle, txtExitSlot, btnExit });

            leftPanel.Controls.AddRange(new Control[] { lblUserDetail, lblParkingForm, lblPlate, txtPlateNumber,
                                                     lblType, cmbVehicleType, lblSelectSlot, cmbSelectSlot,
                                                     btnPark, btnRefreshSlots, exitPanel });

            // Right Panel - All Slots Grid (Both Free and Occupied)
            Panel rightPanel = new Panel() { Location = new Point(330, 0), Size = new Size(730, 560), BackColor = Color.FromArgb(28, 40, 70), BorderStyle = BorderStyle.FixedSingle };

            Label lblSlotsTitle = new Label()
            {
                Text = "PARKING SLOTS STATUS",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 10),
                Size = new Size(700, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(15, 25, 50)
            };

            dgvFreeSlots = new DataGridView()
            {
                Location = new Point(15, 50),
                Size = new Size(700, 490),
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvFreeSlots.SelectionChanged += DgvFreeSlots_SelectionChanged;

            rightPanel.Controls.AddRange(new Control[] { lblSlotsTitle, dgvFreeSlots });

            mainPanel.Controls.Add(leftPanel);
            mainPanel.Controls.Add(rightPanel);

            this.Controls.Add(header);
            this.Controls.Add(mainPanel);
        }

        private void RefreshFreeSlots()
        {
            // Get ALL slots (both free and occupied)
            var allSlots = manager.GetAllSlots();
            string vehicleType = cmbVehicleType.SelectedItem?.ToString() ?? "Car";

            DataTable dt = new DataTable();
            dt.Columns.Add("SlotNumber", typeof(string));
            dt.Columns.Add("FloorNo", typeof(int));
            dt.Columns.Add("SlotType", typeof(string));
            dt.Columns.Add("Status", typeof(string));
            dt.Columns.Add("Rate", typeof(string));
            dt.Columns.Add("VehicleInfo", typeof(string));

            foreach (var slot in allSlots)
            {
                decimal baseRate = vehicleType == "Car" ? 50 : (vehicleType == "Bike" ? 20 : 100);
                decimal totalRate = baseRate * (1 + slot.PremiumPercent / 100);

                string status = slot.IsAvailable ? "FREE" : "OCCUPIED";
                string vehicleInfo = "";

                if (!slot.IsAvailable && slot.ParkedVehicle != null)
                {
                    vehicleInfo = slot.ParkedVehicle.PlateNumber + " (" + slot.ParkedVehicle.GetVehicleType() + ")";
                }

                string displaySlot = slot.SlotNumber;
                if (!slot.IsAvailable && slot.ParkedVehicle != null)
                {
                    displaySlot = slot.SlotNumber + " [BUSY]";
                }

                dt.Rows.Add(
                    displaySlot,
                    slot.FloorNo,
                    slot.SlotType,
                    status,
                    totalRate.ToString("0.00"),
                    vehicleInfo
                );
            }

            dgvFreeSlots.DataSource = null;
            dgvFreeSlots.DataSource = dt;

            // Format columns
            if (dgvFreeSlots.Columns.Contains("SlotNumber"))
                dgvFreeSlots.Columns["SlotNumber"].HeaderText = "Slot";
            if (dgvFreeSlots.Columns.Contains("FloorNo"))
                dgvFreeSlots.Columns["FloorNo"].HeaderText = "Floor";
            if (dgvFreeSlots.Columns.Contains("SlotType"))
                dgvFreeSlots.Columns["SlotType"].HeaderText = "Type";
            if (dgvFreeSlots.Columns.Contains("Status"))
                dgvFreeSlots.Columns["Status"].HeaderText = "Status";
            if (dgvFreeSlots.Columns.Contains("Rate"))
                dgvFreeSlots.Columns["Rate"].HeaderText = "Rate (Rs./hr)";
            if (dgvFreeSlots.Columns.Contains("VehicleInfo"))
                dgvFreeSlots.Columns["VehicleInfo"].HeaderText = "Parked Vehicle";

            // Color coding for rows
            foreach (DataGridViewRow row in dgvFreeSlots.Rows)
            {
                if (row.Cells["Status"].Value.ToString() == "FREE")
                {
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.LightSalmon;
                }
            }

            // Populate dropdown with ONLY free slots (for parking)
            cmbSelectSlot.Items.Clear();
            foreach (var slot in allSlots.Where(s => s.IsAvailable))
            {
                cmbSelectSlot.Items.Add(slot.SlotNumber);
            }
            if (cmbSelectSlot.Items.Count > 0)
                cmbSelectSlot.SelectedIndex = 0;
        }

        private void DgvFreeSlots_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvFreeSlots.SelectedRows.Count > 0)
            {
                // Extract just the slot number (remove [BUSY] if present)
                string slotDisplay = dgvFreeSlots.SelectedRows[0].Cells["SlotNumber"].Value?.ToString();
                string slotNumber = slotDisplay?.Replace(" [BUSY]", "").Trim();

                if (!string.IsNullOrEmpty(slotNumber))
                {
                    // Only show in dropdown if it's free
                    var allSlots = manager.GetAllSlots();
                    var slot = allSlots.FirstOrDefault(s => s.SlotNumber == slotNumber);
                    if (slot != null && slot.IsAvailable)
                    {
                        cmbSelectSlot.SelectedItem = slotNumber;
                    }
                }
            }
        }

        private void UpdateStats()
        {
            lblRevenue.Text = $"Total Revenue: Rs. {manager.GetTotalRevenue()}";
        }

        private void BtnPark_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPlateNumber.Text))
            {
                MessageBox.Show("Please enter license plate number!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbSelectSlot.SelectedItem == null)
            {
                MessageBox.Show("Please select a slot!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedSlot = cmbSelectSlot.SelectedItem.ToString();
            string vehicleType = cmbVehicleType.SelectedItem.ToString();

            bool success = manager.ParkVehicle(txtPlateNumber.Text, vehicleType, manager.GetCurrentUser().FullName);

            if (success)
            {
                MessageBox.Show($"Vehicle Parked Successfully at Slot {selectedSlot}!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtPlateNumber.Clear();
                RefreshFreeSlots();
                UpdateStats();
            }
            else
            {
                MessageBox.Show("Parking failed! No slots available or invalid!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RefreshFreeSlots();
            }
        }
    }

    // 10. ADMIN DASHBOARD
    public class AdminDashboard : Form
    {
        private DataGridView dgvUsers;
        private ListBox lstActiveVehicles;
        private Label lblRevenue, lblTotalUsers, lblActiveVehicles;
        private Button btnRefresh, btnLogout, btnReport;
        private ParkingManager manager;

        public AdminDashboard()
        {
            manager = ParkingManager.Instance;
            SetupUI();
            RefreshData();
        }

        private void SetupUI()
        {
            this.Text = "Admin Dashboard - Parking System";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(28, 40, 70);

            // Header
            Panel header = new Panel() { Location = new Point(0, 0), Size = new Size(1000, 70), BackColor = Color.FromArgb(15, 25, 50) };

            Label lblTitle = new Label()
            {
                Text = "ADMIN PANEL",
                Font = new Font("Arial", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(400, 15),
                Size = new Size(200, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnLogout = new Button()
            {
                Text = "LOGOUT",
                Location = new Point(900, 20),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(200, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLogout.Click += (s, e) => {
                LoginForm login = new LoginForm();
                login.Show();
                this.Close();
            };

            header.Controls.AddRange(new Control[] { lblTitle, btnLogout });

            // Stats Panel
            Panel statsPanel = new Panel() { Location = new Point(10, 80), Size = new Size(960, 60), BackColor = Color.FromArgb(15, 25, 50) };

            lblRevenue = new Label()
            {
                Text = "Total Revenue: Rs. 0",
                Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = Color.LightGreen,
                Location = new Point(20, 15),
                Size = new Size(220, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblTotalUsers = new Label()
            {
                Text = "Registered Users: 0",
                Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = Color.LightBlue,
                Location = new Point(380, 15),
                Size = new Size(220, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblActiveVehicles = new Label()
            {
                Text = "Active Vehicles: 0",
                Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = Color.Yellow,
                Location = new Point(740, 15),
                Size = new Size(200, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            statsPanel.Controls.AddRange(new Control[] { lblRevenue, lblTotalUsers, lblActiveVehicles });

            // Users Section
            Label lblUsersTitle = new Label()
            {
                Text = "REGISTERED USERS",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 155),
                Size = new Size(200, 25)
            };

            dgvUsers = new DataGridView()
            {
                Location = new Point(10, 185),
                Size = new Size(960, 200),
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false
            };

            // Active Vehicles Section
            Label lblVehiclesTitle = new Label()
            {
                Text = "ACTIVE VEHICLES",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 405),
                Size = new Size(200, 25)
            };

            lstActiveVehicles = new ListBox()
            {
                Location = new Point(10, 435),
                Size = new Size(700, 150),
                Font = new Font("Courier New", 10),
                BackColor = Color.White
            };

            // Report Button
            btnReport = new Button()
            {
                Text = "GENERATE REPORT",
                Location = new Point(730, 435),
                Size = new Size(240, 150),
                BackColor = Color.FromArgb(0, 150, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 12, FontStyle.Bold)
            };
            btnReport.Click += (s, e) => new Admin().GenerateReport();

            // Refresh Button
            btnRefresh = new Button()
            {
                Text = "REFRESH DATA",
                Location = new Point(10, 600),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnRefresh.Click += (s, e) => RefreshData();

            this.Controls.Add(header);
            this.Controls.Add(statsPanel);
            this.Controls.Add(lblUsersTitle);
            this.Controls.Add(dgvUsers);
            this.Controls.Add(lblVehiclesTitle);
            this.Controls.Add(lstActiveVehicles);
            this.Controls.Add(btnReport);
            this.Controls.Add(btnRefresh);
        }

        private void RefreshData()
        {
            // Get data directly from database
            DataTable usersTable = DatabaseHelper.ExecuteQuery("sp_GetAllUsers");

            // Bind users table to grid  <-- YE LINES NAYI HAIN
            dgvUsers.DataSource = null;
            dgvUsers.DataSource = usersTable;
            if (dgvUsers.Columns.Contains("UserPassword"))
                dgvUsers.Columns["UserPassword"].Visible = false;

            // Update stats labels
            lblRevenue.Text = $"Total Revenue: Rs. {manager.GetTotalRevenue()}";
            lblTotalUsers.Text = $"Registered Users: {usersTable.Rows.Count}";
            lblActiveVehicles.Text = $"Active Vehicles: {manager.GetActiveVehiclesCount()}";



            // Load active vehicles
            lstActiveVehicles.Items.Clear();
            lstActiveVehicles.Items.Add("Slot     Plate Number     Vehicle Type     Owner Name");
            lstActiveVehicles.Items.Add("======================================================");

            foreach (ParkingSlot slot in manager.GetAllSlots())
            {
                if (!slot.IsAvailable && slot.ParkedVehicle != null)
                {
                    lstActiveVehicles.Items.Add($"{slot.SlotNumber,-8} {slot.ParkedVehicle.PlateNumber,-15} " +
                        $"{slot.ParkedVehicle.GetVehicleType(),-12} {slot.ParkedVehicle.OwnerName}");
                }
            }

            if (manager.GetActiveVehiclesCount() == 0)
            {
                lstActiveVehicles.Items.Add("No active vehicles found.");
            }
        }
    }
    // 11. PROGRAM START
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LoginForm());
        }
    }
}