using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace WixAdd
{
    public class Controller
    {
        public XDocument Read(string filename)
        {
            XDocument doc =  XDocument.Parse(File.ReadAllText(filename));

            return doc;
        }

        public void Save(XDocument doc, string filename)
        {
            doc.Save(filename);
        }

        public void addToDirectoryRef(string projectPath, XElement dRef, FileInfo fi, string suffix, XNamespace ns)
        {
            // make a new component id
            string id = fi.Name.Replace(".", "");
            id += suffix;
            string source = calculateRelative(projectPath, fi);

            // first, check. Is there something already there?

            if (dRef.Descendants(ns + "File").Any( f => f.Attribute("Source").Value == source.ToUpper()))
            {
                return; // identical
            }
            if (dRef.Descendants(ns + "Component").Any( c => c.Attribute("Id").Value.ToUpper() == id.ToUpper()))
            {
                // this sucks...
                System.Windows.Forms.MessageBox.Show("The component id: " + id + " already exists for DirectoryRef " + dRef.Attribute("Id").Value + " skipping.");

                return;
            }
         
            // ends with the same file...
            if (dRef.Descendants(ns + "File").Any( f=> Path.GetFileName(f.Attribute("Source").Value).ToUpper() == fi.Name.ToUpper()))
            {
                if (System.Windows.Forms.MessageBox.Show("The file " + fi.Name + " already exists in this DirectoryRef. Confirm that you want to add it?", "File Exists", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                {
                    return;
                }
            }
            XElement component = new XElement( ns + "Component", new XAttribute("Id", id), new XAttribute("Guid", Guid.NewGuid().ToString()), new XAttribute("DiskId", "1"));
            

            
            XElement file = new XElement(ns + "File", new XAttribute("Id", id), new XAttribute("Name", fi.Name), new XAttribute("Source", source));

            component.Add(file);
            dRef.Add(component);

        }

        public void AddCompToFeature(XElement feature, XElement component, XNamespace ns)
        {
            // check if it is already there.
            if (feature.Descendants(ns + "ComponentRef").Any(c => c.Attribute("Id").Value == component.Attribute("Id").Value)) return;

            XElement compRef = new XElement(ns + "ComponentRef", new XAttribute("Id", component.Attribute("Id").Value));

            feature.Add(compRef);

        }

        private string calculateRelative(string projPath, FileInfo fi)
        {
            string relative = GetRelativePath(projPath, fi.FullName);

            // now we need to replace the beginning with.
            relative = relative.TrimStart('\\');
            relative = "$(var.ProjectDir)\\" + relative;

            return relative;

        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fromPath"/> or <paramref name="toPath"/> is <c>null</c>.</exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath))
            {
                throw new ArgumentNullException("fromPath");
            }

            if (string.IsNullOrEmpty(toPath))
            {
                throw new ArgumentNullException("toPath");
            }

            Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
            Uri toUri = new Uri(AppendDirectorySeparatorChar(toPath));

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            }

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            // Append a slash only if the path is a directory and does not have a slash.
            if (!Path.HasExtension(path) &&
                !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }
    }
}
