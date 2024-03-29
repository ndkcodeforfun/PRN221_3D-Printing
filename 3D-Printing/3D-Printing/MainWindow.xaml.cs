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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Assimp;
using HelixToolkit.Wpf;
using Microsoft.Win32;
using Assimp.Configs;
using System.Drawing.Printing;
using System.Printing;
using System.Diagnostics;

namespace _3D_Printing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ModelImporter importer;
        private ModelVisual3D currentModelVisual;
        private System.Timers.Timer printerScanTimer;
        private string selectedPrinter = null;

        public MainWindow()
        {
            InitializeComponent();

            // Add mouse event handlers for camera control
            helixViewport.MouseDown += HelixViewport_MouseDown;
            helixViewport.MouseMove += HelixViewport_MouseMove;
            helixViewport.MouseWheel += HelixViewport_MouseWheel;

            importer = new ModelImporter();

            StartPrinterScanTimer();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Add CameraChanged event handler
            helixViewport.CameraChanged += HelixViewport_CameraChanged;
        }

        private void HelixViewport_CameraChanged(object sender, RoutedEventArgs e)
        {
            // Retrieve camera's position
            Point3D cameraPosition = helixViewport.Camera.Position;

            // Update UI to display coordinates
            UpdateCoordinatesUI(cameraPosition);
        }

        private void UpdateCoordinatesUI(Point3D position)
        {
            // Display X, Y, Z coordinates
            txtXCoordinate.Text = $"X: {position.X}";
            txtYCoordinate.Text = $"Y: {position.Y}";
            txtZCoordinate.Text = $"Z: {position.Z}";
        }


        private string filePath; // Add this variable to store the file path

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Supported Files|*.obj|OBJ Files (*.obj)|*.obj";
            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName; // Save the file path
                string fileExtension = System.IO.Path.GetExtension(filePath).ToLower();

                if (fileExtension == ".obj")
                {
                    LoadFile(filePath);
                }
                else
                {
                    string objFilePath = ConvertToObj(filePath);
                    if (objFilePath != null)
                    {
                        LoadFile(objFilePath);
                    }
                    else
                    {
                        MessageBox.Show("File conversion failed.");
                    }
                }
            }
        }


        private void LoadFile(string filePath)
        {
            try
            {
                var model = importer.Load(filePath);

                // Clear old model
                if (currentModelVisual != null)
                {
                    helixViewport.Children.Remove(currentModelVisual);
                    currentModelVisual = null;
                }

                // Wrap the loaded model in a ModelVisual3D object
                currentModelVisual = new ModelVisual3D();
                currentModelVisual.Content = model;

                // Add the ModelVisual3D to the viewport
                helixViewport.Children.Add(currentModelVisual);

                // Adjust camera to view the model
                helixViewport.ZoomExtents();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading file: " + ex.Message);
            }
        }

        private string ConvertToObj(string filePath)
        {
            try
            {
                // Initialize Assimp
                AssimpContext assimpContext = new AssimpContext();

                // Import the file
                Scene scene = assimpContext.ImportFile(filePath, PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs);

                // Create the new file path with "_converted" appended to the file name
                string newFileName = System.IO.Path.GetFileNameWithoutExtension(filePath) + "_converted.obj";
                string objFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filePath), newFileName);

                // Save the scene as .obj file
                assimpContext.ExportFile(scene, objFilePath, "obj");

                return objFilePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error converting file: " + ex.Message);
                return null;
            }
        }

        private Point lastMousePosition;

        private void HelixViewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                lastMousePosition = e.GetPosition(helixViewport);
            }
        }

        private void HelixViewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(helixViewport);
                double deltaX = currentPosition.X - lastMousePosition.X;
                double deltaY = currentPosition.Y - lastMousePosition.Y;

                // Perform camera translation
                helixViewport.Camera.Position = new Point3D(
                    helixViewport.Camera.Position.X - deltaX * 0.01,
                    helixViewport.Camera.Position.Y + deltaY * 0.01,
                    helixViewport.Camera.Position.Z
                );

                lastMousePosition = currentPosition;
            }
        }

        private void HelixViewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Perform camera zooming
            helixViewport.Camera.Position = new Point3D(
                helixViewport.Camera.Position.X,
                helixViewport.Camera.Position.Y,
                helixViewport.Camera.Position.Z + e.Delta * 0.001
            );
        }


        private void StartPrinterScanTimer()
        {
            // Create a timer with 10 seconds interval
            printerScanTimer = new System.Timers.Timer(3000);
            printerScanTimer.Elapsed += PrinterScanTimer_Elapsed;
            printerScanTimer.AutoReset = true;
            printerScanTimer.Start();
        }

        private void PrinterScanTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Update the list of printers
            Dispatcher.Invoke(() =>
            {
                PopulatePrinterComboBox();
            });
        }

        private void PopulatePrinterComboBox()
        {
            // Get the currently selected printer, if any
            string previouslySelectedPrinter = selectedPrinter;

            // Clear existing printers
            string currentSelection = cbPrinters.SelectedItem as string;
            cbPrinters.Items.Clear();

            // Enumerate all printers and add them to the ComboBox
            LocalPrintServer printServer = new LocalPrintServer();
            foreach (PrintQueue printQueue in printServer.GetPrintQueues())
            {
                cbPrinters.Items.Add(printQueue.Name);
            }

            // Restore previous selection if it still exists in the list
            if (!string.IsNullOrEmpty(previouslySelectedPrinter) && cbPrinters.Items.Contains(previouslySelectedPrinter))
            {
                selectedPrinter = previouslySelectedPrinter;
            }

            // Set the ComboBox selection
            if (cbPrinters.Items.Contains(currentSelection))
            {
                cbPrinters.SelectedItem = currentSelection;
            }
            else if (cbPrinters.Items.Count > 0)
            {
                cbPrinters.SelectedIndex = 0; // Select the first item if the previous selection is not available
                selectedPrinter = cbPrinters.SelectedItem as string;
            }
        }

        private void cbPrinters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update the selected printer
            selectedPrinter = cbPrinters.SelectedItem as string;
        }

        private string ConvertToStl(string filePath)
        {
            try
            {
                // Initialize Assimp
                AssimpContext assimpContext = new AssimpContext();

                // Import the file
                Scene scene = assimpContext.ImportFile(filePath, PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs);

                // Get the desktop path
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                // Create the new file path with ".stl" extension on desktop
                string newFileName = System.IO.Path.GetFileNameWithoutExtension(filePath) + ".stl";
                string stlFilePath = System.IO.Path.Combine(desktopPath, newFileName);

                // Save the scene as .stl file
                assimpContext.ExportFile(scene, stlFilePath, "stl");

                return stlFilePath;
            }
            catch (Exception ex)
            {
                // Ném ra ngoại lệ để xử lý ở phần gọi của phương thức
                throw new Exception("Error converting file: " + ex.Message);
            }
        }


        //private void PrintModel(string stlFilePath)
        //{
        //    try
        //    {
        //        ProcessStartInfo info = new ProcessStartInfo();
        //        info.FileName = "meshlab.exe"; // Đổi thành đường dẫn của ứng dụng bạn muốn sử dụng
        //        info.Arguments = stlFilePath;
        //        info.CreateNoWindow = true;
        //        info.WindowStyle = ProcessWindowStyle.Hidden;

        //        Process.Start(info);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error printing: " + ex.Message);
        //    }
        //}



        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (currentModelVisual != null && !string.IsNullOrEmpty(selectedPrinter))
            {
                try
                {
                    string stlFilePath = ConvertToStl(filePath); // Chuyển đổi sang định dạng .stl
                    if (stlFilePath != null)
                    {
                        MessageBox.Show("Conversion completed: " + stlFilePath);
                        // Không cần gọi phương thức PrintModel ở đây
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error converting: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Please import a 3D model before converting.");
            }
        }






    }
}
