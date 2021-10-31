### Разобрать каждое требование на составляющие (актор, команда, событие, query). Определить, как все бизнес цепочки будут выглядеть и на какие шаги они будут разбиваться.

Набросок в Miro
[Miro](https://miro.com/app/board/o9J_lmy13sg=/)

#### Common

Авторизация в дешборде X должна выполняться через общий сервис аутентификации UberPopug Inc.

Actor: Account\
Command: Login\
Data: User Claims ???\
Event: Account.Logined

Конец дня

Actor: NotifierService\
Command: Complete working day\
Data: Date\
Event: Day.Completed

#### Task Tracker

Новые таски может создавать кто угодно (администратор, начальник, разработчик, менеджер и любая другая роль). 
У задачи должны быть описание, статус (выполнена или нет) и попуг, на которого заассайнена задача.

Actor: Account\
Command: Create Task\
Data: Task (Priced)\
Event: Task.Created

Менеджеры или администраторы должны иметь кнопку «заассайнить задачи», 
которая возьмёт все открытые задачи и рандомно заассайнит каждую на любого из сотрудников. 

Actor: Account (Admin, Manager)\
Command: Reassign Tasks\
Data: Task, PublicAccountID\
Event: Task.Reassigned

Каждый сотрудник должен иметь возможность отметить задачу выполненной.

Actor: Account\
Command: Complete Task\
Data: Task, PublicAccountID\
Event: Task.Completed

#### Accounting

Списание за назначение задачи

Actor: Task.Assigned\
Command: Debet\
Data: Balance, PublicAccountID\
Event: Account.Debeted

Заплатить за выполнение

Actor: Task.Completed\
Command: Credit\
Data: Balance, PublicAccountID\
Event: Account.Credited

В конце дня необходимо считать сколько денег сотрудник получил за рабочий день
    
Actor: Day.Completed\
Command: Calculate balance\
Data: Total day balance\
Event: Balance.calculated

В конце дня необходимо отправлять на почту сумму выплаты

Actor: Balance.calculated\
Command: Send mail\
Data: Total day balance\
Event: ??? no

После выплаты баланса (в конце дня) он должен обнуляться, и в аудитлоге всех операций аккаунтинга должно быть отображено, что была выплачена сумма

Actor: Balance.calculated\
Command: Roll balance\
Data: Total day balance\
Event: ??? no

#### Analytics

Нужно указывать, сколько заработал топ-менеджмент за сегодня

Actor: Account.Debeted, Account.Credited\
Command: Calculate analytics balance\
Data: Managers balance\
Event: ???

Нужно указывать, сколько попугов ушло в минус.

Actor: Account.Debeted, Account.Credited\
Command: Calculate negative balances popugs(\
Data: Losers count\
Event: ???

Нужно показывать самую дорогую задачу за день, неделю или месяц.

Actor: Account.Credited\
Command: Update max award\
Data: Amount\
Event: ???

### Построить модель данных для системы и модель доменов.

![Data Model](https://github.com/hotdroider/aTES/blob/main/aTES%20data.drawio.svg)

### Определить, какие общие данные нужны для разных доменов и как связаны данные между разными доменами.

Информация об аккаунте нужна и для тасок и для биллинга

### Разобраться, какие сервисы, кроме тудушника, будут в нашей системе и какие между ними могут быть связи (как синхронные, так и асинхронные).

![Services](https://github.com/hotdroider/aTES/blob/main/aTES%20services.drawio.svg)

Service - Domain схема + служебный нотификатор о событиях, закрытие дня или инфа о выплате

Все связи между сервисами сделаем асинхронными, 
все данные которые встречаются в разных доменах будем реплицировать через CUD-events

Синхронно юзер будет только получать данные.

### Определить все бизнес события, необходимые для работы системы

Событие | Продюсер | Консумер
------------ | ------------- | -----------
Account.Logined | Auth | Task, Accounting, Analytics
Day.Completed | Notification |  Accounting
Task.Created | Task | Accounting
Task.Assigned | Task | Accounting
Task.Completed | Task | Accounting
Account.Debeted | Accounting | Analytics
Account.Credited | Accounting | Analytics
Account.DayCompleted | Accounting | Notification


### Выписать все CUD события и какие данные нужны для этих событий, которые необходимы для работы системы

Событие | Продюсер | Консумер
------------ | ------------- | -----------
CUD for Account | Auth | Task, Accounting
CUD for Task | Task |  Accounting
