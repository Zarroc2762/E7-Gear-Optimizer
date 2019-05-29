namespace E7_Gear_Optimizer
{
    partial class SelectItemDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dgv_Inventory = new System.Windows.Forms.DataGridView();
            this.c_set = new System.Windows.Forms.DataGridViewImageColumn();
            this.c_Type = new System.Windows.Forms.DataGridViewImageColumn();
            this.c_Grade = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_ILvl = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_Enhance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_Main = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_Value = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_ATKPer = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_ATK = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_SPD = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_CHC = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_CHD = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_HPPer = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_HP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_DEFPer = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_DEF = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_EFF = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_RES = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_Eq = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_SetID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_TypeID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c_ItemID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_Inventory)).BeginInit();
            this.SuspendLayout();
            // 
            // dgv_Inventory
            // 
            this.dgv_Inventory.AllowUserToAddRows = false;
            this.dgv_Inventory.AllowUserToDeleteRows = false;
            this.dgv_Inventory.AllowUserToOrderColumns = true;
            this.dgv_Inventory.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv_Inventory.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgv_Inventory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_Inventory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.c_set,
            this.c_Type,
            this.c_Grade,
            this.c_ILvl,
            this.c_Enhance,
            this.c_Main,
            this.c_Value,
            this.c_ATKPer,
            this.c_ATK,
            this.c_SPD,
            this.c_CHC,
            this.c_CHD,
            this.c_HPPer,
            this.c_HP,
            this.c_DEFPer,
            this.c_DEF,
            this.c_EFF,
            this.c_RES,
            this.c_Eq,
            this.c_SetID,
            this.c_TypeID,
            this.c_ItemID});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgv_Inventory.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgv_Inventory.Location = new System.Drawing.Point(12, 12);
            this.dgv_Inventory.MultiSelect = false;
            this.dgv_Inventory.Name = "dgv_Inventory";
            this.dgv_Inventory.ReadOnly = true;
            this.dgv_Inventory.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dgv_Inventory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_Inventory.Size = new System.Drawing.Size(1094, 439);
            this.dgv_Inventory.TabIndex = 2;
            this.dgv_Inventory.DoubleClick += new System.EventHandler(this.Dgv_Inventory_DoubleClick);
            // 
            // c_set
            // 
            this.c_set.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_set.HeaderText = "Set";
            this.c_set.Name = "c_set";
            this.c_set.ReadOnly = true;
            this.c_set.Width = 29;
            // 
            // c_Type
            // 
            this.c_Type.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_Type.HeaderText = "Type";
            this.c_Type.Name = "c_Type";
            this.c_Type.ReadOnly = true;
            this.c_Type.Width = 37;
            // 
            // c_Grade
            // 
            this.c_Grade.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_Grade.HeaderText = "Grade";
            this.c_Grade.Name = "c_Grade";
            this.c_Grade.ReadOnly = true;
            this.c_Grade.Width = 61;
            // 
            // c_ILvl
            // 
            this.c_ILvl.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_ILvl.HeaderText = "ILvl";
            this.c_ILvl.Name = "c_ILvl";
            this.c_ILvl.ReadOnly = true;
            this.c_ILvl.Width = 49;
            // 
            // c_Enhance
            // 
            this.c_Enhance.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_Enhance.HeaderText = "Enhance";
            this.c_Enhance.Name = "c_Enhance";
            this.c_Enhance.ReadOnly = true;
            this.c_Enhance.Width = 75;
            // 
            // c_Main
            // 
            this.c_Main.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_Main.HeaderText = "Main";
            this.c_Main.Name = "c_Main";
            this.c_Main.ReadOnly = true;
            this.c_Main.Width = 55;
            // 
            // c_Value
            // 
            this.c_Value.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_Value.HeaderText = "Value";
            this.c_Value.Name = "c_Value";
            this.c_Value.ReadOnly = true;
            this.c_Value.Width = 59;
            // 
            // c_ATKPer
            // 
            this.c_ATKPer.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_ATKPer.HeaderText = "ATK%";
            this.c_ATKPer.Name = "c_ATKPer";
            this.c_ATKPer.ReadOnly = true;
            this.c_ATKPer.Width = 61;
            // 
            // c_ATK
            // 
            this.c_ATK.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_ATK.HeaderText = "ATK";
            this.c_ATK.Name = "c_ATK";
            this.c_ATK.ReadOnly = true;
            this.c_ATK.Width = 53;
            // 
            // c_SPD
            // 
            this.c_SPD.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_SPD.HeaderText = "SPD";
            this.c_SPD.Name = "c_SPD";
            this.c_SPD.ReadOnly = true;
            this.c_SPD.Width = 54;
            // 
            // c_CHC
            // 
            this.c_CHC.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_CHC.HeaderText = "Crit";
            this.c_CHC.Name = "c_CHC";
            this.c_CHC.ReadOnly = true;
            this.c_CHC.Width = 47;
            // 
            // c_CHD
            // 
            this.c_CHD.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_CHD.HeaderText = "CritDmg";
            this.c_CHD.Name = "c_CHD";
            this.c_CHD.ReadOnly = true;
            this.c_CHD.Width = 69;
            // 
            // c_HPPer
            // 
            this.c_HPPer.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_HPPer.HeaderText = "HP%";
            this.c_HPPer.Name = "c_HPPer";
            this.c_HPPer.ReadOnly = true;
            this.c_HPPer.Width = 55;
            // 
            // c_HP
            // 
            this.c_HP.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_HP.HeaderText = "HP";
            this.c_HP.Name = "c_HP";
            this.c_HP.ReadOnly = true;
            this.c_HP.Width = 47;
            // 
            // c_DEFPer
            // 
            this.c_DEFPer.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_DEFPer.HeaderText = "DEF%";
            this.c_DEFPer.Name = "c_DEFPer";
            this.c_DEFPer.ReadOnly = true;
            this.c_DEFPer.Width = 61;
            // 
            // c_DEF
            // 
            this.c_DEF.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_DEF.HeaderText = "DEF";
            this.c_DEF.Name = "c_DEF";
            this.c_DEF.ReadOnly = true;
            this.c_DEF.Width = 53;
            // 
            // c_EFF
            // 
            this.c_EFF.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_EFF.HeaderText = "EFF";
            this.c_EFF.Name = "c_EFF";
            this.c_EFF.ReadOnly = true;
            this.c_EFF.Width = 51;
            // 
            // c_RES
            // 
            this.c_RES.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_RES.HeaderText = "RES";
            this.c_RES.Name = "c_RES";
            this.c_RES.ReadOnly = true;
            this.c_RES.Width = 54;
            // 
            // c_Eq
            // 
            this.c_Eq.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.c_Eq.HeaderText = "Equipped";
            this.c_Eq.Name = "c_Eq";
            this.c_Eq.ReadOnly = true;
            this.c_Eq.Width = 77;
            // 
            // c_SetID
            // 
            this.c_SetID.HeaderText = "SetID";
            this.c_SetID.Name = "c_SetID";
            this.c_SetID.ReadOnly = true;
            this.c_SetID.Visible = false;
            // 
            // c_TypeID
            // 
            this.c_TypeID.HeaderText = "TypeID";
            this.c_TypeID.Name = "c_TypeID";
            this.c_TypeID.ReadOnly = true;
            this.c_TypeID.Visible = false;
            // 
            // c_ItemID
            // 
            this.c_ItemID.HeaderText = "ItemID";
            this.c_ItemID.Name = "c_ItemID";
            this.c_ItemID.ReadOnly = true;
            this.c_ItemID.Visible = false;
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Location = new System.Drawing.Point(411, 457);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button2.Location = new System.Drawing.Point(616, 457);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 4;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // SelectItemDialog
            // 
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.button2;
            this.ClientSize = new System.Drawing.Size(1121, 488);
            this.ControlBox = false;
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.dgv_Inventory);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SelectItemDialog";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Select item";
            this.Shown += new System.EventHandler(this.SelectItemDialog_Shown);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.SelectItemDialog_KeyPress);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_Inventory)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv_Inventory;
        private System.Windows.Forms.DataGridViewImageColumn c_set;
        private System.Windows.Forms.DataGridViewImageColumn c_Type;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_Grade;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_ILvl;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_Enhance;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_Main;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_Value;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_ATKPer;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_ATK;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_SPD;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_CHC;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_CHD;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_HPPer;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_HP;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_DEFPer;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_DEF;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_EFF;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_RES;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_Eq;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_SetID;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_TypeID;
        private System.Windows.Forms.DataGridViewTextBoxColumn c_ItemID;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}