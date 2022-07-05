using Renga;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Renga_FollowUsersActions
{
    public class init_app : Renga.IPlugin
    {
        private static Renga.IApplication renga_app = null;
        private Renga.SelectionEventSource select_event;
        private List<Renga.ActionEventSource> m_eventSources = new List<Renga.ActionEventSource>();

        private static Dictionary<string, List<string>> rasdel2users = new Dictionary<string, List<string>>();
        private static List<Guid> non_checked_objects_type = new List<Guid>();
        private static string project_file_path = null;
        private static Guid our_property_id = Guid.Parse("94f7fd69-2c9f-4c44-80b8-36524ab29a18");
       
        //private static string UserSID = null;
        private static List<string> permitted_rasdels = new List<string>();
        
        private enum status_continue_process :int
        {
            AppStart = 0,
            ObjectAttrIsEmpty = 1,
            AllAttrsNonEmpty = 2,
            UsersListInit = 3
        }
        private static int current_progress = (int)status_continue_process.AppStart;
        bool Renga.IPlugin.Initialize(string pluginFolder)
        {
            renga_app = new Renga.Application();
            Renga.IUI renga_user_interface = renga_app.UI;
            Renga.IUIPanelExtension panel_extension = renga_user_interface.CreateUIPanelExtension();

            if (renga_app.Project == null) return false;
            else
            {
                //Если проект открыт, и это первый проект для запущенного приложения Renga
                if (project_file_path == null)
                {
                    project_file_path = renga_app.Project.FilePath;
                    ActionsOnStartProject();
                }
                else if (project_file_path == renga_app.Project.FilePath)
                {
                    //просто продолжаем
                }
                else
                {
                    //Если раннее уже был запущен какой-то проект
                    renga_user_interface.ShowMessageBox(MessageIcon.MessageIcon_Info, "Предупреждение",
                        "Вы открыли новый проект. Изменения в предыдущем проекте будут утеряны");
                    project_file_path = renga_app.Project.FilePath;
                    ActionsOnStartProject();
                }
            }

            //Регистрация кнопок
            //Кнопка выбора файла сопоставления раздела и SID пользователей, а также
            //пропускаемых категорий объектов (Guid)
            //как-то реализовать пропуск категорий и сейчас пока заполним условно
            string UserSID = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;

            non_checked_objects_type = new List<Guid>();
            non_checked_objects_type.Add(Renga.ObjectTypes.Beam);
            non_checked_objects_type.Add(Renga.ObjectTypes.Window);
            non_checked_objects_type.Add(Renga.ObjectTypes.Plate);

            Renga.IAction action_select_roles = renga_user_interface.CreateAction();
            action_select_roles.DisplayName = "Выбор файла ролей";
            Renga.ActionEventSource event_for_button_select_roles = 
                new Renga.ActionEventSource(action_select_roles);
            
            event_for_button_select_roles.Triggered += (sender, args) =>
            {
                OpenFileDialog select_file = new OpenFileDialog();
                if (select_file.ShowDialog() == DialogResult.OK)
                {
                    string[] data = System.IO.File.ReadAllLines(select_file.FileName);
                    rasdel2users = new Dictionary<string, List<string>>();
                    foreach (string str in data)
                    {
                        string[] str_array = str.Split(',');
                        rasdel2users.Add(str_array[0], str_array[1].Split(';').ToList());

                        if (rasdel2users[str_array[0]].Contains(UserSID))
                        {
                            permitted_rasdels.Add(str_array[0]);
                        }
                    }
                }
            };
            //Добавляем событие в число отслеживаемых программойЫ
            m_eventSources.Add(event_for_button_select_roles);
            panel_extension.AddToolButton(action_select_roles);

            //Кнопка-запуск валидации модели
            Renga.IAction action_validate_model = renga_user_interface.CreateAction();
            action_validate_model.DisplayName = "Проверка модели на корректность";
            Renga.ActionEventSource event_for_button_validate_model =
                new Renga.ActionEventSource(action_select_roles);
            event_for_button_validate_model.Triggered += (sender, args) =>
            {
                if (current_progress == (int)status_continue_process.AllAttrsNonEmpty &&
                rasdel2users.Count() >= 1)
                {
                    current_progress = (int)status_continue_process.UsersListInit;
                    renga_app.UI.ShowMessageBox(MessageIcon.MessageIcon_Info, "Объявление",
                        "Всё корректно, работу можно начать!");
                }
                else
                {
                    //Повторить процедуру выбора файла сопоставления и регистрацию свойств в модели
                }
            };
            panel_extension.AddToolButton(action_validate_model);

            renga_user_interface.AddExtensionToPrimaryPanel(panel_extension);
            renga_user_interface.AddExtensionToActionsPanel(panel_extension,
                Renga.ViewType.ViewType_View3D);
            //Отслеживание выбора

            var selection = renga_app.Selection;
            if (selection == null) return false;
            
            else
            {
                select_event = new SelectionEventSource(selection);
                select_event.ModelSelectionChanged += (sender, args) =>
                {
                    WorkWithSelectedObjects(selection.GetSelectedObjects());
                };
               
                
            }
            return true;
        }
        void Renga.IPlugin.Stop()
        {
            //Правила хорошего тона -- очищаем пользовательские события
            foreach (var eventSource in m_eventSources)
                eventSource.Dispose();
            m_eventSources.Clear();
        }
        private void WorkWithSelectedObjects(Array data)
        {
            List<int> els = data.OfType<int>().ToList();
            List<int> els_non = new List<int>();
            var model_objects_coll = renga_app.Project.Model.GetObjects();
            foreach (int el_id in els)
            {
                Renga.IModelObject model_obj = model_objects_coll.GetById(el_id);
                Renga.IProperty our_prop = model_obj.GetProperties().Get(our_property_id);
                if (our_prop.HasValue())
                {
                    if (permitted_rasdels.Contains(our_prop.GetEnumerationValue()))
                    {
                        //ok
                    }
                    else
                    {
                        els_non.Add(el_id);
                        //как-то снять выбор с объекта
                    }
                }
                else
                {
                    renga_app.UI.ShowMessageBox(MessageIcon.MessageIcon_Error, "Предупреждение",
                        $"Для объекта {model_obj.Name} среди выбранных не заполнено отслеживаемое свойство");
                }
            }

            if (els_non.Any())
            {
                //Снимаем выделение с того что пользователю нельзя трогать
                //и выкидываем предупреждение
                
            }
        }
        private static void ActionsOnStartProject()
        {
            //Проверка, есть ли в проекте зарегистрированное свойство для Разделов
            Renga.IPropertyManager prop_man = renga_app.Project.PropertyManager;
            if (prop_man.IsPropertyRegistered(our_property_id))
            {
                AssignPropToObjects();
            }
            else
            {
                Renga.PropertyDescription descr = new PropertyDescription();
                descr.Name = "Раздел_Слежение";
                descr.Type = PropertyType.PropertyType_Enumeration;
                prop_man.RegisterProperty(our_property_id, descr);
                AssignPropToObjects();
            }
            void AssignPropToObjects()
            {
                foreach (KeyValuePair<string, Guid> name2prop in ObjectTypes())
                {
                    //Исключение на общие типы (Балки, Отверстия, Пластины) используемые всеми
                    if (!non_checked_objects_type.Contains(name2prop.Value) && !prop_man.IsPropertyAssignedToType(our_property_id, name2prop.Value))
                    {
                        prop_man.AssignPropertyToType(our_property_id, name2prop.Value);
                    }
                }
            }
            //Проверка объектов, заполнено ли свойство
            List<int> model_objects_ids_empty = new List<int>();
            var models_objects = renga_app.Project.Model.GetObjects();
            for (int counter_objects = 0; counter_objects < models_objects.Count; counter_objects ++)
            {
                Renga.IModelObject obj = models_objects.GetByIndex(counter_objects);
                Renga.IProperty obj_prop = obj.GetProperties().Get(our_property_id);
                if (!obj_prop.HasValue()) model_objects_ids_empty.Add(obj.Id);
                
            }
            if (model_objects_ids_empty.Any())
            {
                current_progress = (int)status_continue_process.ObjectAttrIsEmpty;
                //renga_app.Selection.SetSelectedObjects(model_objects_ids_empty.ToArray());
                Renga.IModelView modelView = renga_app.ActiveView as Renga.IModelView;
                if (modelView != null)
                {
                    List<int> models_objects_ids = ((int[])models_objects.GetIds()).ToList();
                    modelView.SetObjectsVisibility(models_objects_ids.Except(model_objects_ids_empty.ToArray()).ToArray(), false);
                    models_objects_ids.Clear();
                    renga_app.UI.ShowMessageBox(MessageIcon.MessageIcon_Info, "Объявление",
                        "В модели присутствуют объекты, которым не назначено значение Раздел_Слежение." +
                        "В настоящий момент эти объекты выделены в модели - повторите назначение ещё раз");
                }
            }
            else 
            {
                current_progress = (int)status_continue_process.AllAttrsNonEmpty;
            }
        }
        
        private static Dictionary<string, Guid> ObjectTypes()
        {
            return new Dictionary<string, Guid>
            {
                { "AssemblyInstance",   Renga.ObjectTypes.AssemblyInstance},
                { "Axis",   Renga.ObjectTypes.Axis},
                { "Beam",   Renga.ObjectTypes.Beam},
                { "Column", Renga.ObjectTypes.Column},
                { "Dimension",  Renga.ObjectTypes.Dimension},
                { "Door",   Renga.ObjectTypes.Door},
                { "Duct",   Renga.ObjectTypes.Duct},
                { "DuctAccessory",  Renga.ObjectTypes.DuctAccessory},
                { "DuctFitting", Renga.ObjectTypes.DuctFitting},
                { "ElectricDistributionBoard",  Renga.ObjectTypes.ElectricDistributionBoard},
                { "Element",  Renga.ObjectTypes.Element},
                { "Elevation",  Renga.ObjectTypes.Elevation},
                { "Equipment",  Renga.ObjectTypes.Equipment},
                { "Floor",  Renga.ObjectTypes.Floor},
                { "Hatch",  Renga.ObjectTypes.Hatch},
                { "IfcObject",  Renga.ObjectTypes.IfcObject},
                { "IsolatedFoundation", Renga.ObjectTypes.IsolatedFoundation},
                { "Level",  Renga.ObjectTypes.Level},
                { "LightFixture",   Renga.ObjectTypes.LightFixture},
                { "Line3D", Renga.ObjectTypes.Line3D},
                { "LineElectricalCircuit",  Renga.ObjectTypes.LineElectricalCircuit},
                { "MechanicalEquipment", Renga.ObjectTypes.MechanicalEquipment},
                { "Opening",   Renga.ObjectTypes.Opening},
                { "Pipe",   Renga.ObjectTypes.Pipe},
                { "PipeAccessory",  Renga.ObjectTypes.PipeAccessory},
                { "PipeFitting",  Renga.ObjectTypes.PipeFitting},
                { "Plate",  Renga.ObjectTypes.Plate},
                { "PlumbingFixture",  Renga.ObjectTypes.PlumbingFixture},
                { "Railing", Renga.ObjectTypes.Railing},
                { "Ramp",   Renga.ObjectTypes.Ramp},
                { "Rebar",  Renga.ObjectTypes.Rebar},
                { "Roof",   Renga.ObjectTypes.Roof},
                { "Room",   Renga.ObjectTypes.Room},
                { "Route",  Renga.ObjectTypes.Route},
                { "RoutePoint", Renga.ObjectTypes.RoutePoint},
                { "Section", Renga.ObjectTypes.Section},
                { "Stair",  Renga.ObjectTypes.Stair},
                { "TextShape",  Renga.ObjectTypes.TextShape},
                { "Undefined",  Renga.ObjectTypes.Undefined},
                { "Wall",   Renga.ObjectTypes.Wall},
                { "WallFoundation", Renga.ObjectTypes.WallFoundation},
                { "Window", Renga.ObjectTypes.Window},
                { "WiringAccessory",Renga.ObjectTypes.WiringAccessory},
            };
        }
    }
}
