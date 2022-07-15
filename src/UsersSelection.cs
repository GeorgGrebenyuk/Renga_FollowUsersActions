using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Renga;

namespace Follow_actions
{
    public partial class UsersSelection : Form
    {
        public UsersSelection()
        {
            InitializeComponent();
            //Отображение в форме типов объектов Renga
            dataGridView1.ColumnCount = 1;
            dataGridView1.Columns[0].Name = "Типы объектов в Renga";
            dataGridView1.Columns[0].Width = 350;

            foreach (KeyValuePair<string,Guid> objtype2id in Res.ObjectTypes())
            {
                dataGridView1.Rows.Add(objtype2id.Key);
            }
            radioButton1.Checked = true;
            textBox1.Text = String.Join(";", Res.design_sections().ToArray());
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        /// <summary>
        /// Выбор категорий объектов модели для игнорирования при работе плагина
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            List<string> selected_categories = new List<string>();
            if (checkBox1.Checked) init_app.no_following_object_types = new List<Guid>();
            List<KeyValuePair<string, Guid>> obj_types = Res.ObjectTypes().ToList();
            int selectedRowCount = dataGridView1.Rows.GetRowCount(DataGridViewElementStates.Selected);
            if (selectedRowCount > 0)
            {
                
                for (int i = 0; i < selectedRowCount; i++)
                {
                    var data = obj_types.ToList()[0];
                    KeyValuePair<string, Guid> finded = obj_types[dataGridView1.SelectedRows[i].Index];
                    init_app.no_following_object_types.Add(finded.Value);
                    selected_categories.Add(finded.Key);
                }
                init_app.renga_app.UI.ShowMessageBox(MessageIcon.MessageIcon_Info, "Сообщение",
                    "Были выбраны следующие категории объектов модели: \n" + String.Join("\n", selected_categories.ToArray()));
            }
            int temp0 = 0;
            
        }
        /// <summary>
        /// Запуск проверки модели на корректность для начала
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            //Проверка пункта 1 - есть ли свойство в модели
            Renga.IPropertyManager prop_man = init_app.renga_app.Project.PropertyManager;
            if (!prop_man.IsPropertyRegistered(init_app.our_property_id))
            {
                //Создаем это свойство и регистрируем в проекте
                Renga.IPropertyDescription descr = prop_man.CreatePropertyDescription("Раздел_Слежение",
                    Renga.PropertyType.PropertyType_Enumeration);

                string text_box = textBox1.Text;
                descr.SetEnumerationItems(text_box.Split(';'));
                prop_man.RegisterProperty2(init_app.our_property_id, descr);
            }
            int temp1 = 1;
            //Проверка пункт 2 - назначено ли свойство категориям объектов модели
            List<Guid> needing_obj_types;
            if (init_app.no_following_object_types == null | (init_app.no_following_object_types != null && init_app.no_following_object_types.Count() == 0))
            {
                //Учитываем все категории объектов
                needing_obj_types = Res.ObjectTypes().Select(a => a.Value).ToList();
            }
            //Учитываем все за минусом обозначенных пользователем
            else needing_obj_types = Res.ObjectTypes().Select(a => a.Value).ToList().Except(init_app.no_following_object_types).ToList();

            foreach (Guid obj_type in needing_obj_types)
            {
                if (!prop_man.IsPropertyAssignedToType(init_app.our_property_id, obj_type)) 
                    prop_man.AssignPropertyToType(init_app.our_property_id, obj_type);
            }
            int temp2 = 2;
            //Проверка пункта 3 - заполнено ли свойство для всех объектов 
            List<int> model_objects_ids_empty = new List<int>();
            List<int> hide_too = new List<int>();
            List<int> all_model_objects = new List<int>();
            Renga.IModelObjectCollection model_objects = init_app.renga_app.Project.Model.GetObjects();
            for (int counter_objects = 0; counter_objects < model_objects.Count; counter_objects++)
            {
                
                Renga.IModelObject one_object = model_objects.GetByIndex(counter_objects);
                all_model_objects.Add(one_object.Id);
                Guid obj_type = Guid.Parse(one_object.ObjectTypeS);
                if (needing_obj_types.Contains(obj_type))
                {
                    Renga.IProperty obj_prop = one_object.GetProperties().Get(init_app.our_property_id);
                    if (obj_prop!= null && needing_obj_types.Contains(obj_type) && 
                        obj_prop.HasValue() && obj_prop.GetEnumerationValue() == "_no")
                    {
                        model_objects_ids_empty.Add(one_object.Id);
                    }
                }
                //Если тип объекта модели входит в список с неучитываемыми категориями (их тоже скрываем по дефолту)
                //else if (!needing_obj_types.Contains(obj_type)) hide_too.Add(one_object.Id);


            }
            if (model_objects_ids_empty.Any())
            {
                int message_box_return_type = Res.MessageBox(IntPtr.Zero, "В модели имеются объекты, " +
                    "которым не назначено свойство принадлежности к разделу проектирования. Если вы нажмете на 'Нет' - то они останутся выделенными для" +
                    "редактирования свойства. Если нажмете на 'Да' - то плагин начнет работу при новом выделении объектов", "Предупреждение",
                    Res.MB_ICONQUESTION | Res.MB_YESNO | Res.MB_DEFBUTTON1);
                if (message_box_return_type == Res.IDNO)
                {
                    //Cancel => Show objects
                    //List<int> all_hided = model_objects_ids_empty.Concat(hide_too).ToList();
                    List<int> objects_to_hide = all_model_objects.Except(model_objects_ids_empty).ToList();
                    Renga.IModelView mview = init_app.renga_app.ActiveView as Renga.IModelView;
                    if (mview != null)
                    {
                        mview.SetObjectsVisibility(objects_to_hide.ToArray(), false);
                    }
                    else init_app.renga_app.UI.ShowMessageBox(Renga.MessageIcon.MessageIcon_Error, "Ошибка", "Запустите эту опцию из 3д-вида");
                }
                else
                {
                    init_app.can_start_following = true;
                }
            }
            else
            {
                //Удивительная ситуация :) все заполнено
                init_app.can_start_following = true;
            }
            int temp3 = 3;
            //Считывание выбранных режимов работы с выборкой объектов
            if (radioButton1.Checked) init_app.mode_selection = 1;
            else if (radioButton2.Checked) init_app.mode_selection = 2;
            else if (radioButton3.Checked) init_app.mode_selection = 3;
            this.Close();

        }
        

        /// <summary>
        /// Выбор файла сопоставления параметров
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog select_user_roles = new OpenFileDialog();
            string user_sid = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
            if (select_user_roles.ShowDialog() == DialogResult.OK)
            {
                string file_path = select_user_roles.FileName;
                if (File.Exists(file_path))
                {
                    string[] file_data = File.ReadAllLines(file_path);
                    foreach (string one_string in file_data)
                    {
                        if (one_string.Split('\t')[0] == user_sid)
                        {
                            IEnumerable<string> users_design_section = one_string.Split('\t')[1].Split(';');
                            if (users_design_section.Any()) init_app.permitted_design_sections = users_design_section.ToList();
                            else return;
                        }
                    }
                }
                else return;
            }
            else return;

            if (init_app.permitted_design_sections == null)
            {
                init_app.renga_app.UI.ShowMessageBox(MessageIcon.MessageIcon_Warning, "Ошибка",
                    "Не обнаружена позиция сопоставления данного идентификатора " +
                    "пользователя с разрешенными для редактирования разделами");
            }
            int temp0 = 0;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button_save_settings_Click(object sender, EventArgs e)
        {

        }
        //Вывод сюда списка сокращений разделов
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/GeorgGrebenyuk/Renga_FollowUsersActions");
        }
    }
}
