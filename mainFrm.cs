using FSUIPC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSTimeSync
{
    public partial class mainFrm : AppFormBase
    {
        public mainFrm()
        {
            InitializeComponent();
            RegisterMoveableControl(topLabel);

            topLabel.Text = "FS Time Sync v" + FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;
            BackColor = Color.FromArgb(133, 187, 92);
            btnConnect.Text = "Connect";

            forceUtc = false;

            tmrMinute = new Timer();
            tmrMinute.Interval = 1000;
            tmrMinute.Tick += TmrMinute_Tick;
            tmrMinute.Start();
        }

        private void TmrMinute_Tick(object sender, EventArgs e)
        {
            if (forceUtc)
            {
                ForceSimToUTC(EnvironmentDateTime);
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            switch (btnConnect.Text)
            {
                case "Connect":
                case "Nop. Connect again":
                    if (ConnectToSim())
                        btnConnect.Text = "Impose UTC";
                    else
                        btnConnect.Text = "Nop. Try connect again";
                    break;
                case "Impose UTC":
                    forceUtc = true;
                    btnConnect.Text = "Stop imposing UTC";
                    break;
                case "Stop imposing UTC":
                    forceUtc = false;
                    btnConnect.Text = "Impose UTC";
                    break;
            }
        }

        #region local variables
        Timer tmrMinute;

        bool forceUtc;

        private Offset<byte[]> environmentDateTime = new Offset<byte[]>(0x0238, 10);
        static private Offset<byte[]> environmentDateTimeDayOfYear = new Offset<byte[]>(0x023E, 4);
        static private Offset<byte[]> environmentDateTimeHour = new Offset<byte[]>(0x023B, 4);
        static private Offset<byte[]> environmentDateTimeMinute = new Offset<byte[]>(0x023C, 4);
        static private Offset<byte[]> environmentDateTimeYear = new Offset<byte[]>(0x0240, 4);
        private DateTime EnvironmentDateTime
        {
            get
            {
                try
                {
                    FSUIPCConnection.Process();

                    // get year in Simulator
                    short year = BitConverter.ToInt16(environmentDateTime.Value, 8);

                    // create a time based on Jan 1 of Simulator year
                    DateTime result = new DateTime(year, 1, 1, environmentDateTime.Value[0], environmentDateTime.Value[1], environmentDateTime.Value[2]);

                    // get day of year from Simulator, and add that to the above time
                    short dayOfYear = BitConverter.ToInt16(environmentDateTime.Value, 6);

                    // add and return
                    return result.Add(new TimeSpan(dayOfYear - 1, 0, 0, 0));
                }
                catch (FSUIPCException crap)
                {
                    switch (crap.FSUIPCErrorCode)
                    {
                        case FSUIPCError.FSUIPC_ERR_NOTOPEN:
                            break;
                    }

                    return DateTime.MinValue;
                }
            }
            set
            {
                environmentDateTimeDayOfYear.Value = BitConverter.GetBytes(value.DayOfYear);
                environmentDateTimeHour.Value = BitConverter.GetBytes(value.Hour);
                environmentDateTimeMinute.Value = BitConverter.GetBytes(value.Minute);
                environmentDateTimeYear.Value = BitConverter.GetBytes(value.Year);

                try
                {
                    FSUIPCConnection.Process();
                }
                catch (FSUIPCException crap)
                {
                    
                }
            }
        }
        #endregion local variables

        private bool ConnectToSim()
        {
            try
            {
                FSUIPCConnection.Open();

                return true;
            }
            catch (FSUIPCException crap)
            {
                switch (crap.FSUIPCErrorCode)
                {
                    case FSUIPCError.FSUIPC_ERR_NOFS:
                        break;
                    case FSUIPCError.FSUIPC_ERR_OPEN:
                        return true;
                }
            }
            return false;
        }

        private bool ForceSimToUTC(DateTime _sim)
        {
            // adjust dates, since they don't really matter for us
            _sim = _sim.AddDays(Math.Truncate((DateTime.UtcNow - _sim).TotalDays));

            // calculate diff
            TimeSpan timeDiff = (DateTime.UtcNow > _sim) ? DateTime.UtcNow - _sim : _sim - DateTime.UtcNow;

            if (Math.Truncate(timeDiff.TotalMinutes) > 1)
            {
                EnvironmentDateTime = DateTime.UtcNow;
                return true;
            }
            return false;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }
    }
}
