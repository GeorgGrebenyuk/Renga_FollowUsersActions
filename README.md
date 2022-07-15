# Renga_FollowUsersActions
[Тестовая версия!] Плагин для САПР Renga для слежения за действиями пользователей в рамках командной работы над файлом (исключение вероятности изменения чужих элементов).

# Описание
Настоящий плагин является частью расширенного курса по Renga API (середина июля 2022 г.), рассматривается логика работы с объектами модели, типами объектов, объектными свойствами, событиями выбора и открытия проекта. Плагин - это тестовая реализация механики запрета изменений объектов модели чужого раздела (при работе в среде одной модели Renga через Renga Collaboration Server).

## Принцип работы
### Шаг №1
Плагин устанавливается у всех членов рабочей команды и после установки встраивается в Renga и доступен на Главной панели задач с любого вида. 
![Кнопка запуска плагина](docs/image_1.png)

### Шаг №2
При запуске плагина первым делом необходимо выбрать текстовый файл, где будет прописано сопоставлние идентификатора пользователя с разрешенными для него разделами для редактирования.

![Выбор текстового файла с сопоставлением параметров](docs/image_2.png)

Текстовый файл представляет собой построчное указание идентификатора пользователя: системный SID пользователя и перечисленные через ";" разделы проектирования. Пример файла вы можете посмотреть [здесь](demo_materials/).

Логика использования заключается в создании и заполнении нового объектного свойства "Раздел_Слежение" с типом Перечисление (Enumertion), где перечислены Разделы проктирования (АР, КМ, ЭС и пр.), присваиваемые вручную объектам модели (вернее, объектам эти свойства будут автоматически назначены, задачей Разработчика модели будет лишь назначение нужного раздела элементу модели) - 1 раз при первом использовании плагина и потом регулярно при создании новых элементов модели. Значение свойства по умолчанию будет ```_no```, что будет игнорироваться при последующих проверках.

**Примечание:** новое свойство "Раздел_Слежение" будет иметь фиксированный Guid ```94f7fd69-2c9f-4c44-80b8-36524ab29a18``` (сгенерирован на лету через [https://www.guidgenerator.com](https://www.guidgenerator.com)).

После назначения объектам модели нужных свойств (здесь, Разделов) необходимо через кнопку выбора плагина указать текстовый файл формата ```csv``` вида: Раздел,Идентификаторы_системы_пользователя через ";". То есть указать, какой **раздел** данный **пользователь** может изменять, а какой не может. Предупреждая ваш вопрос, как же изначально назначать объектные параметры - я вас "упокою", плагин не начнет работать до тех пор, пока все элементы модели не будут промаркированы этим тегом. Здесь при заполнении и контроле незаполненных параметров подойдет скрипт Dynamo ```some_script.dyn```.
Где "Идентификаторы_системы_пользователя" - это так называемые SID (идентификатор безопасности учетной записи пользователя Windows), получаются [вот так](https://winitpro.ru/index.php/2016/05/27/kak-uznat-sid-polzovatelya-po-imeni-i-naoborot/). ```System.Security.Principal.WindowsIdentity.GetCurrent().User.Value```

Наименование раздела | Имена пользователей рабочих машин
--|--
АР | S-1-5-21-2927053466-1818515551-2824591131—110;S-1-5-21-2927053466-1818515551-2824591131—110;S-1-5-21-1004336348-1177238915-682003330-512
КМ | S-1-5-21-2927053466-1888515551-2824591131—110;S-1-5-21-2927053466-1818515551-2824591131—110;S-1-5-21-1004334348-1177238915-682003330-512
ОВ | S-1-5-21-2427053466-1818515551-2827591131—110;S-1-5-21-1004334348-1177238915-682003330-512

Далее, наконец, можно начинать совместную работу над моделью по следующей логике: Пользователи работают со своих локальных ПК, у каждого пользователя есть ссвой SID для его учетной записи. При выборе пользователем объекта модели будет происходить автосравнение содержимого поля "Раздел" с текстовым файлом "Ролей" и текущим SID пользователя. Если Пользователю разрешено редактировать этот раздел, то он спокойно продолжит работу. Если же запрещено - то будет выскакивать предупреждение и выбор сбрасываться. 
