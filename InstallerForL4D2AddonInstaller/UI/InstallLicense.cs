using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InstallerForL4D2AddonInstaller.UI
{
    public partial class InstallLicense : UserControl
    {
        InstallerForm installerForm;
        public InstallLicense(InstallerForm form)
        {
            InitializeComponent();
            installerForm = form;
        }

        public event EventHandler<bool> AgreementCheckedChanged;

        const string LicenseLink = "https://wiki.dakang233.com/wiki/%E6%B1%82%E7%94%9F%E4%B9%8B%E8%B7%AF2%E9%99%84%E5%8A%A0%E7%BB%84%E4%BB%B6%E5%AE%89%E8%A3%85%E5%99%A8/%E8%AE%B8%E5%8F%AF%E5%8D%8F%E8%AE%AE";

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        private void linkLblLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(LicenseLink);
        }

        private void checkBoxAgreement_CheckedChanged(object sender, EventArgs e)
        {
            AgreementCheckedChanged?.Invoke(this, checkBoxAgreement.Checked);
        }
    }
}
