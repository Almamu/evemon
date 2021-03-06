using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.IO;
using EVEMon.Common.Data;
using EVEMon.Common.Serialization.Datafiles;

namespace EVEMon.Common.Controls
{
    /// <summary>
    /// Displays an image for a given EveObject
    /// </summary>
    /// <remarks>
    /// Setting the PopUpEnabled property to true enables a pop-up window for EveObjects with a 256 x 256 image available, accessed
    /// via the user double-clicking the image.
    /// Image size must be set using the ImageSize property. The default Size property is overriden.
    /// </remarks>
    public partial class EveImage : UserControl
    {

        #region Private Properties
        private bool m_PopUpEnabled;
        private bool m_PopUpActive;
        private EveImageSize m_ImageSize;
        private Item m_item = null;
        #endregion

        #region Public Properties
        public enum EveImageSize { x0 = 0, x16 = 16, x32 = 32, x64 = 64, x128 = 128, x256 = 256 }

        public bool PopUpEnabled
        {
            get { return m_PopUpEnabled; }
            set
            {
                m_PopUpEnabled = value;
            }
        }

        public Item EveItem
        {
            get { return m_item; }
            set
            {
                m_item = value;
                if (m_ImageSize != EveImageSize.x0)
                {
                    GetImage();
                }
            }
        }

        public EveImageSize ImageSize
        {
            get { return m_ImageSize; }
            set
            {
                m_ImageSize = value;
                pbImage.Size = new Size((int)m_ImageSize, (int)m_ImageSize);
                this.Size = pbImage.Size;
            }
        }

        new public Size Size
        {
            get { return base.Size; }
            set
            {
                base.Size = new Size((int)m_ImageSize, (int)m_ImageSize);
            }
        }

        #endregion

        #region Constructor
        /// <summary>
        /// Initialise the control
        /// </summary>
        /// <remarks>
        /// The default image size is 64 x 64, with the image pop-up enabled.
        /// </remarks>
        public EveImage()
        {
            InitializeComponent();
            SetImageTypeAttributes();
            ImageSize = EveImageSize.x64;
            PopUpEnabled = true;
            ShowBlankImage();
        }

        #endregion

        #region Image type configuration
        /// <summary>
        /// Identifies the image type being handled
        /// </summary>
        private enum ImageType { Ship, Drone, Structure, Item, None }

        /// <summary>
        /// Indicates the source of the .png image name
        /// </summary>
        private enum ImageNameFrom { TypeID, Icon };

        /// <summary>
        /// Holds configuration data for different image types
        /// </summary>
        private Dictionary<ImageType, ImageTypeData> m_ImageTypeAttributes;

        /// <summary>
        /// Defines configuration data for a specific ImageType
        /// </summary>
        private struct ImageTypeData
        {
            public string localComponent;
            public string urlPath;
            public ImageNameFrom imageNameFrom;
            public ArrayList validSizes;

            public ImageTypeData(string local, string url, ImageNameFrom name, ArrayList sizes)
            {
                localComponent = local;
                urlPath = url;
                imageNameFrom = name;
                validSizes = sizes;
            }
        }

        /// <summary>
        /// Builds the m_ImageTypeAttributes dictionary
        /// </summary>
        private void SetImageTypeAttributes()
        {
            ArrayList validSizes;
            m_ImageTypeAttributes = new Dictionary<ImageType, ImageTypeData>();
            // Ships
            validSizes = new ArrayList();
            validSizes.Add(EveImageSize.x32);
            validSizes.Add(EveImageSize.x64);
            validSizes.Add(EveImageSize.x128);
            validSizes.Add(EveImageSize.x256);
            m_ImageTypeAttributes.Add(ImageType.Ship, new ImageTypeData("Ships", "icons", ImageNameFrom.TypeID, validSizes));
            // Items
            validSizes = new ArrayList();
            validSizes.Add(EveImageSize.x16);
            validSizes.Add(EveImageSize.x32);
            validSizes.Add(EveImageSize.x64);
            validSizes.Add(EveImageSize.x128);
            m_ImageTypeAttributes.Add(ImageType.Item, new ImageTypeData("Items", "icons", ImageNameFrom.Icon, validSizes));
            // Drones
            validSizes = new ArrayList();
            validSizes.Add(EveImageSize.x32);
            validSizes.Add(EveImageSize.x64);
            validSizes.Add(EveImageSize.x128);
            validSizes.Add(EveImageSize.x256);
            m_ImageTypeAttributes.Add(ImageType.Drone, new ImageTypeData("Drones", "icons", ImageNameFrom.TypeID, validSizes));
            // Structures
            validSizes = new ArrayList();
            validSizes.Add(EveImageSize.x32);
            validSizes.Add(EveImageSize.x64);
            validSizes.Add(EveImageSize.x128);
            validSizes.Add(EveImageSize.x256);
            m_ImageTypeAttributes.Add(ImageType.Structure, new ImageTypeData("", "icons", ImageNameFrom.TypeID, validSizes));
        }

        private ImageType GetImageType(Item item)
        {
            switch (item.Family)
            {
                case ItemFamily.Ship:
                    return ImageType.Ship;
                case ItemFamily.Drone:
                    return ImageType.Drone;
                case ItemFamily.StarbaseStructure:
                    return ImageType.Structure;
                default:
                    return ImageType.Item;
            }
        }

        #endregion

        #region Image Retrieval and Pop Up
        /// <summary>
        /// Renders a BackColor square as a placeholder for the image
        /// </summary>
        private void ShowBlankImage()
        {
            Bitmap b = new Bitmap(pbImage.ClientSize.Width, pbImage.ClientSize.Height);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.FillRectangle(new SolidBrush(BackColor), new Rectangle(0, 0, b.Width, b.Height));
            }
            pbImage.Image = b;
        }

        /// <summary>
        /// Retrieves image for the given EveObject
        /// </summary>
        private void GetImage()
        {
            // Set to black image initially
            ShowBlankImage();

            // Reset flags and cursor
            m_PopUpActive = false;
            toolTip1.Active = false;
            pbImage.Cursor = Cursors.Default;

            if (m_item != null)
            {
                ImageType imageType = GetImageType(m_item);
                ImageTypeData typeData = m_ImageTypeAttributes[imageType];

                // Only display an image if the correct size is available
                if (typeData.validSizes.Contains(m_ImageSize))
                {
                    // Set file & pathname variables
                    string eveSize = ((int)m_ImageSize).ToString() + "_" + ((int)m_ImageSize).ToString();

                    string imageWebName;
                    string imageResourceName;
                    if (typeData.imageNameFrom == ImageNameFrom.TypeID)
                    {
                        imageWebName = m_item.ID.ToString();
                        imageResourceName = "_" + imageWebName;
                    }
                    else
                    {
                        imageWebName = "icon" + m_item.Icon.ToString();
                        imageResourceName = imageWebName;
                    }

                    // Try and get image from a local optional resources file (probably don't used anymore, not sure)
                    string localResources = String.Format(
                        "{1}Resources{0}Optional{0}{2}{3}.resources",
                        Path.DirectorySeparatorChar,
                        System.AppDomain.CurrentDomain.BaseDirectory,
                        typeData.localComponent,
                        eveSize);

                    bool foundResource = false;
                    try
                    {
                        if (System.IO.File.Exists(localResources))
                        {
                            System.Resources.IResourceReader basic;
                            basic = new System.Resources.ResourceReader(localResources);
                            System.Collections.IDictionaryEnumerator basicx = basic.GetEnumerator();
                            while (basicx.MoveNext())
                            {
                                if (basicx.Key.ToString() == imageResourceName)
                                {
                                    pbImage.Image = (System.Drawing.Image)basicx.Value;
                                    foundResource = true;
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                    }


                    // Try to get image from web (or local cache located in %APPDATA%\EVEMon) if not found yet
                    if (!foundResource)
                    {
                        // Result should be like :
                        // http://eve.no-ip.de/icons/32_32/icon22_08.png
                        // http://eve.no-ip.de/icons/32_32/7538.png
                        string imageURL = String.Format(NetworkConstants.CCPIcons, typeData.urlPath, eveSize, imageWebName);

                        ImageService.GetImageAsync(imageURL, true, (img) => GotImage(m_item.ID, img));
                    }

                    // Enable pop up if required
                    if (m_PopUpEnabled && typeData.validSizes.Contains(EveImageSize.x256))
                    {
                        toolTip1.Active = true;
                        m_PopUpActive = true;
                        pbImage.Cursor = Cursors.Hand;
                    }

                }
            }
        }

        /// <summary>
        /// Callback method for asynchronous web requests
        /// </summary>
        /// <param name="id">EveObject id for retrieved image</param>
        /// <param name="i">Image object retrieved</param>
        private void GotImage(int id, Image i)
        {
            // Only display the image if the id matches the current EveObject
            if (i != null && m_item.ID == id)
            {
                pbImage.Image = i;
            }
        }

        /// <summary>
        /// Event handler for image double click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbImage_DoubleClick(object sender, EventArgs e)
        {
            // Only display the pop up form if pop-ups are enabled and a suitable image can be retrieved
            if (m_PopUpActive)
            {
                EveImagePopUp popup = new EveImagePopUp(m_item);
                popup.FormClosed += delegate { popup.Dispose(); };
                popup.Show();
            }
        }
        #endregion

    }
}
