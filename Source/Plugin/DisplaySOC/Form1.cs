using System;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Windows.Forms.DataVisualization.Charting;

namespace DisplaySOC
{
    public partial class Form1 : Form
    {
        List<SocObject> _socData;
        string _name = String.Empty;
        string _unit = String.Empty;
        int _timer = 0;

        //inputData looks like: Title, Units, InitalSoc0, FinalSoc0, InitalSoc1, FinalSoc1, ... InitalSocN, FinalSocN
        public Form1(string[] inputData)
        {
            if (inputData == null || inputData.Length < 6) return;
            HandleInputData(inputData);
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            FormatGui();
            timer1.Start();
        }

        #region HandleData

        private void HandleInputData(string[] inputData)
        {
            _name = inputData[0];
            _unit = inputData[1];
            SocSort.AppendUnitsToColumnNames(_unit);
            List<double> socData = new List<double>();
            for (int i = 2; i < inputData.Length; i++) socData.Add(TypeCast.ToDouble(inputData[i]));
            HandleSocData(socData.ToArray());
        }

        private void HandleSocData(double[] socData)
        {
            _socData = new List<SocObject>();
            double maxCharge = socData[0];
            double deltaSOC = 0;
            for (int i = 0; i < socData.Length / 2; i++)
            {
                deltaSOC = socData[i * 2] - socData[i * 2 + 1];
                _socData.Add(new SocObject(i, maxCharge, socData[i*2], socData[(i*2) + 1], deltaSOC));
            }
            for(int i = 0; i < _socData.Count - 1; i++) _socData[i].CalculateDepletion(_socData.Last().FinalSOC.Value);
            _socData.Last().Depletion.Value = "-";
            _socData.Last().Reccomendation.Value = "-";
        }

        private string Truncate(string input, int decimalPlaces)
        {
            if (!TypeCast.IsDouble(input)) return input;
            return (Math.Round(TypeCast.ToDouble(input), decimalPlaces)).ToString();
        }

        #endregion

        #region Window

        private void FormatGui()
        {
            this.Size = new Size(1481, 589);
            FormatChart(chart1);
            FormatDataGrid();
            ResizeChart();
            ResizeDataGrid();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            ResizeChart();
            ResizeDataGrid();
        }

        #endregion

        #region Chart

        private void FormatChart(Chart chart)
        {
            chart.Series[0].Points.Clear();
            for (int i = 0; i < _socData.Count; i++)
            {
                chart.Series[0].Points.Add(new DataPoint( _socData[i].CycleNumber.Value, _socData[i].FinalSOC.Value ));
            }

            chart.Series[0].ChartType = SeriesChartType.Line;
            chart.Series[0].BorderWidth = 2;
            chart.Series[0].MarkerStyle = MarkerStyle.Square;
            chart.Series[0].MarkerSize = 10;
            chart.Legends[0].Enabled = false;

            chart.ChartAreas[0].AxisX.Minimum = 0;
            if(_socData.Count != 1) chart.ChartAreas[0].AxisX.Maximum = _socData.Count - 1;
            chart.ChartAreas[0].AxisY.Minimum = _socData.Select(x => x.FinalSOC.Value).ToList().Min();
            double ymax = _socData.Select(x => x.FinalSOC.Value).ToList().Max();
            if (ymax != chart.ChartAreas[0].AxisY.Minimum) chart.ChartAreas[0].AxisY.Maximum = ymax;

            chart.ChartAreas[0].AxisX.Title = SocSort.CycleNumber;
            chart.ChartAreas[0].AxisY.Title = SocSort.FinalSOC;
            chart.ChartAreas[0].AxisX.TitleFont = new Font("Arial", 12, FontStyle.Regular);
            chart.ChartAreas[0].AxisY.TitleFont = new Font("Arial", 12, FontStyle.Regular);

            chart.ChartAreas[0].AxisX.LabelStyle.Enabled = true;
            chart.ChartAreas[0].AxisY.LabelStyle.Enabled = true;
            chart.ChartAreas[0].AxisX.LabelStyle.Format = "0.###";
            chart.ChartAreas[0].AxisY.LabelStyle.Format = "0.###";

            chart.Titles.Clear();
            chart.Titles.Add(_name);
            chart.Titles[0].Font = new Font("Arial", 12, FontStyle.Regular);
        }

        private void ResizeChart()
        {
            chart1.Size = new Size(Math.Max(0, this.Size.Width - 584), Math.Max(0, (this.Size.Height - 20) - 64));
            chart1.Location = new Point(13, 13 + 20);
        }

        #endregion

        #region Data Grid

        private void FormatDataGrid()
        {
            DataGridViewCellStyle boldStyle = new DataGridViewCellStyle();
            boldStyle.Font = new Font(dataGridView1.Font, FontStyle.Bold);

            DataGridViewCellStyle greenStyle = new DataGridViewCellStyle();
            greenStyle.Font = new Font("Arial", 12, FontStyle.Regular);
            greenStyle.BackColor = Color.Lime;

            dataGridView1.ReadOnly = true;
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();
            for (int i = 0; i < SocSort.ColumnNames.Length; i++)
            {
                dataGridView1.Columns.Add(SocSort.ColumnNames[i].Replace(" ", ""), SocSort.ColumnNames[i]);
                dataGridView1.Columns[i].Width = 70;
                for (int j = 0; j < _socData.Count; j++)
                {
                    if (i == 0)
                    {
                        dataGridView1.Rows.Add();
                        dataGridView1.Rows[j].Height = 25;
                    }
                    object index = _socData[j].SocArray[i];
                    string indexValue = index.GetType().GetProperty("Value").GetValue(index, null).ToString();
                    dataGridView1[i, j].Value = Truncate(indexValue, 3);
                    if (indexValue == SocSort.Pass) dataGridView1[i, j].Style.ApplyStyle(greenStyle);
                }     
            }
            dataGridView1.Rows[_socData.Count - 1].DefaultCellStyle = boldStyle;

            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.AllowUserToResizeColumns = false;
            dataGridView1.AllowDrop = false;
        }

        private void ResizeDataGrid()
        {
            int height = 0;
            for (int i = 0; i < dataGridView1.Rows.Count; i++) height += dataGridView1.Rows[i].Height;
            dataGridView1.Size = new Size(533, Math.Max(0, Math.Min(height + dataGridView1.ColumnHeadersHeight + 2, (this.Size.Height - 20) - 64)));
            dataGridView1.Location = new Point(_socData.Count == 1 ? 13 : this.Size.Width - 560, 13 + 20);
        }

        private void dataGridView1_MouseUp(object sender, MouseEventArgs e)
        {
            ResizeDataGrid();
        }

        #endregion

        #region Toolbar

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormatGui();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            _timer++;
            this.Text = "State of Charge Assessment --- Intra-Test Pause Timer: " + _timer;
        }

        #endregion
    }
}
