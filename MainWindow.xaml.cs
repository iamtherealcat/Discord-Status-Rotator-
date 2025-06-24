using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.VisualBasic; 

namespace DiscordStatusRotator
{
    public partial class MainWindow : Window
    {
        private List<string> statuses = new List<string>();
        private int rotationInterval = 2; 
        private string token = "";
        private bool isRotationActive = false; 

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void LoadConfig()
        {
            
            if (File.Exists("config.json"))
            {
                var json = File.ReadAllText("config.json");
                var config = JsonConvert.DeserializeObject<JObject>(json);
                statuses = config["statuses"].ToObject<List<string>>();
                rotationInterval = (int)config["rtsec"];
                token = config["Token"].ToString();
            }

            
            StatusListBox.ItemsSource = statuses;
            IntervalInput.Text = rotationInterval.ToString();
        }

        private void SaveConfig(object sender, RoutedEventArgs e)
        {
           
            var config = new
            {
                statuses = statuses,
                rtsec = rotationInterval,
                Token = token
            };
            File.WriteAllText("config.json", JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented));
            MessageBox.Show("Config saved successfully!");
        }

        private void AddStatus(object sender, RoutedEventArgs e)
        {
           
            string newStatus = StatusInput.Text;
            if (!string.IsNullOrWhiteSpace(newStatus))
            {
                statuses.Add(newStatus); 
                StatusListBox.Items.Refresh(); 
                StatusInput.Clear();
            }
        }

        private void RemoveStatus(object sender, RoutedEventArgs e)
        {
            
            if (StatusListBox.SelectedItem != null)
            {
                statuses.Remove(StatusListBox.SelectedItem.ToString());
                StatusListBox.Items.Refresh(); 
            }
        }

        private async void ToggleRotation(object sender, RoutedEventArgs e)
        {
            
            if (!isRotationActive)
            {
                
                if (string.IsNullOrWhiteSpace(token))
                {
                    MessageBox.Show("Please enter a valid token.");
                    return;
                }

                
                if (!int.TryParse(IntervalInput.Text, out rotationInterval))
                {
                    MessageBox.Show("Please enter a valid number for the rotation interval.");
                    return;
                }

               
                isRotationActive = true;
                StartStopButton.Content = "Stop Rotation";
                await RotateStatuses();
            }
            else
            {
                
                isRotationActive = false;
                StartStopButton.Content = "Start Rotation"; 
                MessageBox.Show("Rotation Stopped.");
            }
        }

        private async Task RotateStatuses()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", token);

                while (isRotationActive) 
                {
                    foreach (var status in statuses)
                    {
                        var payload = new
                        {
                            status = "online",
                            custom_status = new { text = status }
                        };
                        var content = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");

                        
                        var request = new HttpRequestMessage(new HttpMethod("PATCH"), "https://discord.com/api/v9/users/@me/settings")
                        {
                            Content = content
                        };

                        var response = await client.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Status Rotated: {status}");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to update status: {response.StatusCode}");
                        }

                        await Task.Delay(rotationInterval * 1000);
                    }
                }
            }
        }

        
        private void SetToken(object sender, RoutedEventArgs e)
        {
            var tokenInput = Interaction.InputBox("Please enter your Discord Bot Token:", "Set Token");

            if (!string.IsNullOrWhiteSpace(tokenInput))
            {
                token = tokenInput;
                MessageBox.Show("Token has been set successfully!");
            }
            else
            {
                MessageBox.Show("Invalid Token.");
            }
        }
    }
}
