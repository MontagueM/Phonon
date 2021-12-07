using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phonon
{
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;

    using HelixToolkit.Wpf;

    /// <summary>
    /// Provides a ViewModel for the Main window.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
        {
            Dynamic dynamic = new Dynamic(0);
            dynamic.Vertices = new List<float[]>();
            dynamic.Faces = new List<uint[]>();
            if (!File.Exists("msh.tmp")) return;
            dynamic.ReadMeshData(dynamic.Vertices, dynamic.Faces);
            UpdateModel(dynamic.Vertices, dynamic.Faces);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        //protected void OnPropertyChanged([CallerMemberName] string name = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        //}

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler == null) return;
            handler(this, new PropertyChangedEventArgs(propertyName));
        }
        public void UpdateModel(List<float[]> Vertices, List<uint[]> Faces)
        {
            var modelGroup = new Model3DGroup();
            MeshGeometry3D mesh = new MeshGeometry3D();
            Int32Collection triangleIndices = new Int32Collection();
            Point3DCollection positions = new Point3DCollection();

            foreach (float[] v in Vertices)
            {
                Point3D p = new Point3D(v[0], v[1], v[2]);
                positions.Add(p);
            }

            foreach (uint[] Face in Faces)
            {
                for (int i = 0; i < 3; i++)
                {
                    triangleIndices.Add((int)Face[i]);
                }
            }

            mesh.Positions = positions;
            mesh.TriangleIndices = triangleIndices;

            var Mat = MaterialHelper.CreateMaterial(Colors.White);
            modelGroup.Children.Add(new GeometryModel3D { Geometry = mesh, Transform = new TranslateTransform3D(0, 0, 0), Material = Mat, BackMaterial = Mat });
            this.Model = modelGroup;

            OnPropertyChanged("Model");
        }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        public Model3D Model { get; set; }
    }
}
