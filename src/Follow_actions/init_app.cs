using Renga;
using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Follow_actions
{
    public class init_app : Renga.IPlugin
    {
        public static Renga.IApplication renga_app;
        private List<Renga.ActionEventSource> follow_actions;
        private Renga.SelectionEventSource follow_selection;
        private Renga.ApplicationEventSource follow_application;
        //Plugin data
        public static List<string> permitted_design_sections = null;
        public static List<Guid> no_following_object_types = null;
        public static bool can_start_following = false;
        public static Guid our_property_id = Guid.Parse("94f7fd69-2c9f-4c44-80b8-36524ab29a18");
        public static int mode_selection = 1;
        public bool Initialize(string pluginFolder)
        {
            renga_app = new Renga.Application();
            follow_actions = new List<ActionEventSource>();
            Renga.IUI renga_ui = renga_app.UI;
            Renga.IUIPanelExtension panel = renga_ui.CreateUIPanelExtension();


            init_app.no_following_object_types = new List<Guid>();

            /*Кнопка для запуска диалогового окна для подготовки проекта и его валидации для пригодности начала работы с плагином
                0. Фиксация параметров работы плагина - различные настройки
                1. Выбор текстового файла сопоставления идентификаторов пользователей с разрешенными Идентификаторами для них;
                2. Выбор типов объектов модели, к которым не надо применять отслеживание 
            и выбор подтверждается нажатием на кнопку 'Подтвердить', после чего в справочном фрейме отобразится перечень выбранных категорий
            для игнорирования;
                3. Проверяется, имеется ли наше свойство среди свойств модели и если нет, то добавляется в модель;
                4. Проверяется, назначено ли наше свойство всем (кроме оговоренных) категориям объектов Renga, 
            и если нет - то назначается;
                5. Проверяется, заполнено ли свойство у всех объектов модели (кроме категорий оговоренных выше), 
            и если не заполнено - выскакивает предупреждающее окно, запрашивающее начать ли работу. 
            Если выбирается 'Нет' - то в модели остаются объекты с незаполненными параметрами, остальные (заполненные) скрываются
            и для продолжения Пользователю надо будет заново вызвать эту опцию; 
            в противном случае, при нажатии 'Да', can_start_following = true и плагин начинает работу 
            (при каждом новом выделении пользователем объектов модели)
             */

            Renga.IAction plugin_buton = renga_ui.CreateAction();
            plugin_buton.ToolTip = "Запуск настройки плагина для совместной работы";
            Renga.IImage icon0 = renga_ui.CreateImage();
            icon0.LoadFromFile(pluginFolder + "\\logo_main.png");
            plugin_buton.Icon = icon0;
            ActionEventSource plugin_action_event = new ActionEventSource(plugin_buton);
            plugin_action_event.Triggered += (o, s) =>
            {
                UsersSelection frame = new UsersSelection();
                System.Windows.Forms.Application.Run(frame);
                frame.Close();
            };

            //Отслеживание выбора объектов в интерфейсе Renga
            follow_selection = new SelectionEventSource(renga_app.Selection);
            follow_selection.ModelSelectionChanged += on_selection;

            //Отслеживание статуса проекта
            follow_application = new ApplicationEventSource(renga_app);
            follow_application.ProjectClosed += () =>
            {
                can_start_following = false;
                permitted_design_sections = null;
                no_following_object_types = new List<Guid>();
            };

            panel.AddToolButton(plugin_buton);
            renga_ui.AddExtensionToPrimaryPanel(panel);

            return true;
        }


        public void Stop()
        {
            foreach (var one_action_event in follow_actions) { one_action_event.Dispose(); }
            follow_selection.Dispose();
            follow_application.Dispose();
        }
        

        private void on_selection(object sender, EventArgs args)
        {
            if (!can_start_following) return;
            Renga.ISelection selection = renga_app.Selection;
            List<int> selected_objects_id = selection.GetSelectedObjects().OfType<int>().ToList();

            /* При каждом новом выборе пользователем объектов модели действуют следующие операции:
                1. Получаются идентификаторы объектов модели, выделенных Пользователем;
                2. Если объекты модели не имеют тип Renga.ObjectTypes.Undefined:
                    2.1. Если список с игнорируемыми типами объектов пуст или в нем не содержится объектный тип то идем дальше;
                    2.2. Если у модели заполнена свойство Enumeration и оно != '_no':
                    2.3. Если это свойство не входит в набор свойств, разрешенных для пользователя по файлу сопоставления, то 
                этот идентификатор объекта фиксируется во временный список
                3. В зависимости от выбранной логики работы с выделенными объектами чужих типов, по форме в классе UsersSelection.cs
                аквтивируется соответствующий режим.
             */

            //Список для объектов, которые Пользователь не имеет права трогать
            List<int> wrong_objects = new List<int>();
            List<string> wrong_objects_names = new List<string>();
            Renga.IModelObjectCollection model_objects = init_app.renga_app.Project.Model.GetObjects();
            foreach (int internal_model_object_id in selected_objects_id)
            {
                Renga.IModelObject one_object = model_objects.GetById(internal_model_object_id);
                //Исключение от ошибок
                if (one_object.ObjectType != Renga.ObjectTypes.Undefined)
                {
                    if ((no_following_object_types!= null && !no_following_object_types.Contains(one_object.ObjectType)) |
                        no_following_object_types == null)
                    {
                        Renga.IProperty obj_prop = one_object.GetProperties().Get(init_app.our_property_id);

                        if (obj_prop != null && obj_prop.HasValue() && obj_prop.GetEnumerationValue() != "_no")
                        {
                            if (!permitted_design_sections.Contains(obj_prop.GetEnumerationValue()))
                            {
                                wrong_objects.Add(internal_model_object_id);
                                wrong_objects_names.Add(one_object.Name);
                            }
                        }
                    }  
                }                
            }
            if (wrong_objects.Any())
            {

                List<int> empty_array = new List<int>();
                if (mode_selection == 1)
                {
                    renga_app.Selection.SetSelectedObjects(empty_array.ToArray());
                    renga_app.Selection.SetSelectedObjects(selected_objects_id.Except(wrong_objects).ToArray());
                    renga_app.UI.ShowMessageBox(MessageIcon.MessageIcon_Info, "Сообщение", "Было снято выделение с объектов, " +
                        "которые вам запрещено изменять!");
                }
                else if (mode_selection == 2)
                {
                    renga_app.Selection.SetSelectedObjects(empty_array.ToArray());
                    renga_app.Selection.SetSelectedObjects(selected_objects_id.Except(wrong_objects).ToArray());
                }
                else if (mode_selection == 3)
                {
                    int message_box_return_type = Res.MessageBox(IntPtr.Zero, $"Среди выделенных объектов есть объекты ({wrong_objects.Count()} шт), \n " +
                        String.Join("\n",wrong_objects_names.ToArray()) + 
                        "\n для выбора которых требуется настоящее уведомление. Вы уверены, что хотите продолжить? " +
                        "Если вы нажмете на 'Нет' - то с них выбор снимется. Если нажмете на 'Да' - то выбор сохранится", "Предупреждение",
                    Res.MB_ICONQUESTION | Res.MB_YESNO | Res.MB_DEFBUTTON1);
                    if (message_box_return_type == Res.IDNO)
                    {
                        renga_app.Selection.SetSelectedObjects(empty_array.ToArray());
                        renga_app.Selection.SetSelectedObjects(selected_objects_id.Except(wrong_objects).ToArray());
                    }
                    else
                    {
                        //nothing
                    }
                }



            }
        }
    }
}
