using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.IO;

namespace WixAdd
{
    public partial class Form1 : Form
    {
        private XDocument _doc;
        private string _filename;
        private Controller _controller;
        private static XNamespace _WIXNS = "http://schemas.microsoft.com/wix/2006/wi";

        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                _filename = openFileDialog1.FileName;
                _controller = new Controller();
                _doc = _controller.Read(_filename);

                renderTree();
            }
        }

        private void renderTree()
        {
            treeView1.Nodes.Clear();

            TreeNode root = treeView1.Nodes.Add(System.IO.Path.GetFileName(_filename));

            XElement productNode = _doc.Descendants(_WIXNS + "Product").First();

            TreeNode features = root.Nodes.Add("Features");
            features.BackColor = Color.LightGreen;

            foreach( var elem in productNode.Elements( _WIXNS + "Feature"))
            {
                TreeNode n = features.Nodes.Add(elem.Attribute("Id").Value);
                n.Tag = elem;
                n.ToolTipText = elem.ToString();

                renderFeature(n, elem);
            }

            TreeNode dirRefs = root.Nodes.Add("DirectoryRefs");
            dirRefs.BackColor = Color.LightBlue;

            foreach( var elem in _doc.Descendants( _WIXNS + "DirectoryRef"))
            {
                TreeNode n = dirRefs.Nodes.Add(elem.Attribute("Id").Value);
                n.Tag = elem;
                n.ToolTipText = elem.ToString();

                renderDirRef(n, elem);
            }

            root.Expand();
            foreach (TreeNode child in root.Nodes) child.Expand(); // 2 levels.

        }
        
        private void renderDirRef(TreeNode refNode, XElement elem)
        {
            // clear it...
            refNode.Nodes.Clear();
            refNode.ContextMenuStrip = cmsDirRef;
            

            foreach (var comp in elem.Elements(_WIXNS + "Component"))
            {
                renderComponent(refNode, comp);
            }
        }

        private void renderFeature(TreeNode refNode, XElement elem)
        {
            // clear it...
            refNode.Nodes.Clear();
            refNode.ContextMenuStrip = cmsFeature;

            // find any child features.
            foreach( var sub in elem.Elements(_WIXNS + "Feature"))
            {
                TreeNode n = refNode.Nodes.Add(sub.Attribute("Id").Value);
                n.Tag = sub;
                n.ToolTipText = sub.ToString();
                renderFeature(n, sub);
            }

            foreach (var comp in elem.Elements(_WIXNS + "ComponentRef"))
            {
                renderComponent(refNode, comp);
            }
        }

        private void updateTree(TreeNode dirRef)
        {
            XElement elem = dirRef.Tag as XElement;
            if (elem == null) return;

            if (elem.Name.LocalName.ToUpper() != "DIRECTORYREF") return;

            dirRef.Nodes.Clear();
            renderDirRef(dirRef, elem);
        }

        private void renderComponent(TreeNode parent, XElement component)
        {
            TreeNode comp = parent.Nodes.Add(component.Attribute("Id").Value);
            comp.Tag = component;
            comp.ToolTipText = component.ToString();
            comp.ContextMenuStrip = cmsComponent;

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_doc == null)
            {
                MessageBox.Show("There is no active file.");

            }
            else
            {
                _controller.Save(_doc, _filename);
                MessageBox.Show("Saved.");
            }
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(tbFolder.Text) == false)
            {
                MessageBox.Show("The folder does not exist!");
                return;
            }

            DirectoryInfo di = new DirectoryInfo(tbFolder.Text);
            var files = di.GetFiles();
            renderFiles(files);

        }

        private void renderFiles(FileInfo[] files)
        {
            lbFiles.Items.Clear();

            lbFiles.Items.AddRange(files);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //something has to be selected that is a directoryref.
            if (treeView1.SelectedNode == null)
            {
                MessageBox.Show("Please select a DirectoryRef in the Tree.");
                return;
            }
            XElement elem = treeView1.SelectedNode.Tag as XElement;
            if (elem == null)
            {
                MessageBox.Show("Please select a DirectoryRef in the Tree.");
                return;
            }

            if (elem.Name.LocalName.ToUpper() != "DIRECTORYREF")
            {
                MessageBox.Show("Please select a DirectoryRef in the tree!");
                return;
            }

            if (lbFiles.SelectedItems.Count==0)
            {
                MessageBox.Show("Please select one or more files to add.");
                return;
            }

            foreach( var item in lbFiles.SelectedItems )
            {
                FileInfo fi = item as FileInfo;
                if (fi == null) continue;

                _controller.addToDirectoryRef(Path.GetDirectoryName(_filename), elem, fi, tbSuffix.Text, _WIXNS);
                
            }

            renderDirRef(treeView1.SelectedNode, elem);
        }

        private void onContextOpening(object sender, CancelEventArgs e)
        {
            // clear it.
            cmsFeature.Items.Clear();

            // depending on what type of node it is, offer different actions.
          
        }

        private void clearComponentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode n;
            XElement elem;

            if (getNodeElem("Feature", out n, out elem))
            {
                elem.RemoveNodes();
                n.Nodes.Clear();

                renderFeature(n, elem);
            }
        }

        private bool getNodeElem(string targetName, out TreeNode node, out XElement elem, bool matchType = true)
        {
            node = null;
            elem = null;
            node = treeView1.SelectedNode;
            if (node == null)
            {
                MessageBox.Show("Please select a node");
                return false;
            }

            elem = node.Tag as XElement;
            if (elem == null)
            {
                MessageBox.Show("Please select a node that represents an element in the Wix");
                return false;
            }

            if (matchType == true)
            {
                if (elem.Name.LocalName.ToUpper() == targetName.ToUpper())
                {
                    return true;
                }
                else
                {
                    MessageBox.Show("Please select a node of type: " + targetName);
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode n;
            XElement elem;

            if (getNodeElem("Component", out n, out elem, false))
            {
                if ((elem.Name.LocalName == "Component") || (elem.Name.LocalName == "ComponentRef"))
                {
                    XElement parent = elem.Parent;
                    elem.Remove();
                    TreeNode parentNode = n.Parent;
                    n.Remove();

                    renderDirRef(parentNode, parent);
                }
            }
            
        }

      

        private void Tsmi_Click(object sender, EventArgs e)
        {
            TreeNode n;
            XElement elem;

            bool found = (getNodeElem("DirectoryRef", out n, out elem, false));

            if (found)
            {

                if (elem.Name.LocalName == "DirectoryRef")
                {
                    // get the selected click.
                    ToolStripMenuItem tsmi = sender as ToolStripMenuItem;
                    if (tsmi != null)
                    {
                        XElement feat = tsmi.Tag as XElement;

                        foreach (var comp in elem.Descendants(_WIXNS + "Component"))
                        {
                            _controller.AddCompToFeature(feat, comp, _WIXNS);
                        }
                        // afterward, refresh the feature.
                        TreeNode feature = findNode(treeView1.Nodes[0], feat);
                        if (feature != null) renderFeature(feature, feat);
                    }
                }
                if (elem.Name.LocalName == "Component")
                {
                    // get the selected click.
                    ToolStripMenuItem tsmi = sender as ToolStripMenuItem;
                    if (tsmi != null)
                    {
                        XElement feat = tsmi.Tag as XElement;

                        _controller.AddCompToFeature(feat, elem, _WIXNS);
                       
                        // afterward, refresh the feature.
                        TreeNode feature = findNode(treeView1.Nodes[0], feat);
                        if (feature != null) renderFeature(feature, feat);
                    }
                }
            }

            
        }

        private TreeNode findNode(TreeNode from, XElement elem)
        {
            foreach( TreeNode child in from.Nodes)
            {
                XElement ce = child.Tag as XElement;
                if (ce != null)
                {
                    if (elem.Equals(ce)) return child;
                }
                
                if (child.Nodes.Count>0)
                {
                    TreeNode found = findNode(child, elem);
                    if (found != null) return found;
                }
            }

            return null;
        }

        private void onAddOpening(object sender, CancelEventArgs e)
        {
            addAllComponentsToToolStripMenuItem.DropDownItems.Clear();

            foreach( var feat in _doc.Descendants(_WIXNS + "Feature"))
            {
                var tsmi = addAllComponentsToToolStripMenuItem.DropDownItems.Add(feat.Attribute("Id").Value);
                tsmi.Tag = feat;
                tsmi.Click += Tsmi_Click;
            }
        }

        private void onCMSCompOpening(object sender, CancelEventArgs e)
        {
            addComponentToFeatureToolStripMenuItem.DropDownItems.Clear();

            TreeNode n;
            XElement elem;

            if (getNodeElem("", out n, out elem, false))
            {
                if (elem.Name.LocalName == "Component")
                {
                    addComponentToFeatureToolStripMenuItem.Enabled = true;
                }
                else
                {
                    addComponentToFeatureToolStripMenuItem.Enabled = false;
                }
            }

           

            foreach (var feat in _doc.Descendants(_WIXNS + "Feature"))
            {
                var tsmi = addComponentToFeatureToolStripMenuItem.DropDownItems.Add(feat.Attribute("Id").Value);
                tsmi.Tag = feat;
                tsmi.Click += Tsmi_Click;
            }
        }
    }
}
