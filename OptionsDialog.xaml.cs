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
using System.Windows.Shapes;

namespace ImpactAnalyzer
{
    /// <summary>
    /// Interaction logic for OptionsDialog.xaml
    /// </summary>
    public partial class OptionsDialog : Window
    {
        public ImpactAnalysisParams Params { get; set; }

        private int MaxActivityDefinition = 5;
        
        private double ActivityDefinitionTextBoxHeight = 20;
        private double ActivityDefinitionLabelWidth = 200;
        private double ActivityDefinitionWatermarkWidth = 100;
        private double ActivityDefinitionTextBoxSpacing = 50;
        private double ActivityDefinitionVerticalSpacing = 30;
        private double CanvasStartY = 0;
        private double CanvasStartX = 40;
        private double CanvasFooterHeight = 120;
        private double OkCancelSpacing = 30;

        public OptionsDialog()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void DisplayActivityDefinition()
        {
            this.Height = CanvasStartY + Math.Max(MaxActivityDefinition, Params.ActivityDefinitionList.Count) * (ActivityDefinitionTextBoxHeight + ActivityDefinitionVerticalSpacing) + CanvasFooterHeight;
            this.Width = CanvasStartX + 2 * (ActivityDefinitionWatermarkWidth + ActivityDefinitionTextBoxSpacing) + ActivityDefinitionLabelWidth + ActivityDefinitionTextBoxSpacing;

            okButton.Margin = new Thickness(this.Width - 2 * OkCancelSpacing - cancelButton.Width - okButton.Width,
                                            this.Height - CanvasFooterHeight + CanvasFooterHeight / 3,
                                            0, 0);
            cancelButton.Margin = new Thickness(this.Width - OkCancelSpacing - cancelButton.Width,
                                                this.Height - CanvasFooterHeight + CanvasFooterHeight / 3,
                                                0, 0);

            // Draw ActivityDefinitions
            double Y = CanvasStartY;
            foreach(ActivityDefinition activitydefinition in Params.ActivityDefinitionList)
            {
                TextBox tb = new TextBox();
                tb.Height = ActivityDefinitionTextBoxHeight;
                tb.Width = ActivityDefinitionLabelWidth;
                tb.Margin = new Thickness(CanvasStartX, Y, 0, 0);
                OptionsGrid.Children.Add(tb);

                TextBox tb1 = new TextBox();
                tb1.Height = ActivityDefinitionTextBoxHeight;
                tb1.Width = ActivityDefinitionWatermarkWidth;
                tb1.Margin = new Thickness(CanvasStartX + tb.Width + ActivityDefinitionTextBoxSpacing, Y, 0, 0);
                OptionsGrid.Children.Add(tb1);

                TextBox tb2 = new TextBox();
                tb2.Height = ActivityDefinitionTextBoxHeight;
                tb2.Width = ActivityDefinitionWatermarkWidth;
                tb2.Margin = new Thickness(CanvasStartX + tb.Width + ActivityDefinitionTextBoxSpacing + tb1.Width + ActivityDefinitionTextBoxSpacing, Y, 0, 0);
                OptionsGrid.Children.Add(tb2);

                Y += ActivityDefinitionTextBoxHeight + ActivityDefinitionVerticalSpacing;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayActivityDefinition();
        }
    }
}
