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

namespace _3D_Printing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ModelImporter importer;
        private ModelVisual3D currentModelVisual;

        public MainWindow()
        {
            InitializeComponent();

            // Add mouse event handlers for camera control
            helixViewport.MouseDown += HelixViewport_MouseDown;
            helixViewport.MouseMove += HelixViewport_MouseMove;
            helixViewport.MouseWheel += HelixViewport_MouseWheel;

            importer = new ModelImporter();
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Supported Files|*.obj;*.fbx|OBJ Files (*.obj)|*.obj|FBX Files (*.fbx)|*.fbx";
            if (openFileDialog.ShowDialog() == true)
            {
                LoadFile(openFileDialog.FileName);
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
    }
}
