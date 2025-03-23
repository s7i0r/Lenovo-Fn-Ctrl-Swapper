using System;
using System.Management;
using System.Windows.Forms;
using System.Threading;
using System.Security.Principal;

namespace FnCtrlSwapper
{
    class Program
    {
        static Form waitingForm;
        static Label stateLabel;
        static Button toggleButton;
        static bool isSwapped;

        [STAThread]
        static void Main()
        {
            if (!IsAdmin())
            {
                MessageBox.Show("Please run this application as an administrator.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Application.EnableVisualStyles();

            // Create the main form
            waitingForm = new Form
            {
                Text = "Fn-Ctrl Swapper",
                Width = 300,
                Height = 110,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            // Center the form on the screen
            waitingForm.StartPosition = FormStartPosition.CenterScreen;

            // Add a temporary label for waiting text
            Label waitingLabel = new Label
            {
                Text = "Please wait while we retrieve BIOS settings...",
                AutoSize = true,
                Location = new System.Drawing.Point(30, 20)
            };
            waitingForm.Controls.Add(waitingLabel);

            // Show the form
            waitingForm.Show();

            // Dynamic wait until the FnCtrlKeySwap setting is successfully read
            DateTime startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < 10) // Wait up to 10 seconds
            {
                isSwapped = GetState();
                if (isSwapped != false) // If the state is successfully read
                {
                    break; // Exit the loop
                }
                Thread.Sleep(1000); // Wait for 1 second before checking again
            }

            // Remove the waiting label and add the actual UI elements (state text and toggle button)
            waitingForm.Invoke((MethodInvoker)delegate
            {
                waitingForm.Controls.Clear(); // Clear previous controls

                stateLabel = new Label
                {
                    Text = $"Fn-Ctrl Swapped: {(isSwapped ? "Yes" : "No")}",
                    AutoSize = true,
                    Location = new System.Drawing.Point(70, 10)
                };
                waitingForm.Controls.Add(stateLabel);

                toggleButton = new Button
                {
                    Text = "Toggle Fn-Ctrl Swap",
                    Location = new System.Drawing.Point(30, 40),
                    Width = 200
                };
                toggleButton.Click += (s, e) => ToggleSwap();
                waitingForm.Controls.Add(toggleButton);

                waitingForm.Refresh(); // Ensure UI updates are reflected immediately
            });

            waitingForm.FormClosed += (s, e) => Application.Exit(); // Ensure application exits on form close

            Application.Run();
        }

        static bool IsAdmin()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void ToggleSwap()
        {
            isSwapped = !isSwapped;
            SetState(isSwapped);
            UpdateStateLabel();
        }

        static void UpdateStateLabel()
        {
            stateLabel.Text = $"Fn-Ctrl swapped: {(isSwapped ? "Yes" : "No")}";
        }

        static bool GetState()
        {
            try
            {
                foreach (ManagementObject obj in new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM Lenovo_BiosSetting").Get())
                {
                    string setting = obj["CurrentSetting"]?.ToString();
                    if (!string.IsNullOrEmpty(setting) && setting.Contains("FnCtrlKeySwap"))
                        return setting.Contains("Enable");
                }
            }
            catch { }
            return false; // Default to not swapped if querying fails
        }

        static void SetState(bool enable)
        {
            try
            {
                ExecuteWmiMethod("Lenovo_SetBiosSetting", "SetBiosSetting", $"FnCtrlKeySwap,{(enable ? "Enable" : "Disable")}");
                ExecuteWmiMethod("Lenovo_SaveBiosSettings", "SaveBiosSettings", null);
            }
            catch { }
        }

        static void ExecuteWmiMethod(string className, string methodName, object parameter)
        {
            foreach (ManagementObject obj in new ManagementObjectSearcher(@"root\wmi", $"SELECT * FROM {className}").Get())
                obj.InvokeMethod(methodName, parameter == null ? null : new object[] { parameter });
        }
    }
}
