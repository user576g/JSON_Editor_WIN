using System.Text;
using System.Windows.Forms;

namespace JSON_Editor
{
    public partial class ValidationResultBox : Form
    {
        public ValidationResultBox()
        {
            InitializeComponent();
        }
        internal void SetResult(ValidationResult result)
        {

            var sb = new StringBuilder();
            sb.Append("The file is ");
            if (result.IsValid)
            {
                sb.Append("VALID ");
                pictureBox.Image = Program.checkImg;
            }
            else
            {
                sb.Append("NOT valid ");
                pictureBox.Image = Program.blankImg;
            }
            sb.Append("JSON file!");
            if (!result.IsValid)
            {
                sb.Append("\nThe reason can be somewhere near line ")
                    .Append(result.Row)
                      .Append(", character ").Append(result.At);
            }

            rtbResult.Text = sb.ToString();
        }
    }
}
