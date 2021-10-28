### Разобрать каждое требование на составляющие (актор, команда, событие, query). Определить, как все бизнес цепочки будут выглядеть и на какие шаги они будут разбиваться.

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

У каждого из сотрудников должен быть свой счёт, который показывает, сколько за сегодня он получил денег. 
У счёта должен быть аудитлог того, за что были списаны или начислены деньги, с подробным описанием каждой из задач.

Actor: Account\
Command: Create Task\
Data: Task (Priced)\
Event: Task.Created

Менеджеры или администраторы должны иметь кнопку «заассайнить задачи», 
которая возьмёт все открытые задачи и рандомно заассайнит каждую на любого из сотрудников. 

Actor: Account (Admin, Manager)\
Command: Reassign Tasks\
Data: Task, PublicAccountID\
Event: Task.Assigned

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
Command: Calculate total balance\
Data: Total day balance\
Event: Account.DayCompleted

В конце дня необходимо отправлять на почту сумму выплаты

Actor: Account.DayCompleted\
Command: Send mail\
Data: Total day balance\
Event: ??? no

После выплаты баланса (в конце дня) он должен обнуляться, и в аудитлоге всех операций аккаунтинга должно быть отображено, что была выплачена сумма

Actor: Account.DayCompleted\
Command: Send mail\
Data: Total day balance\
Event: ??? no

#### Analytics

Нужно указывать, сколько заработал топ-менеджмент за сегодня

Actor: Account.Debeted, Account.Credited\
Command: Calculate balance\
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

diagram

### Определить, какие общие данные нужны для разных доменов и как связаны данные между разными доменами.

Информация об аккаунте нужна и для тасок и для биллинга

### Разобраться, какие сервисы, кроме тудушника, будут в нашей системе и какие между ними могут быть связи (как синхронные, так и асинхронные).

### Определить все бизнес события, необходимые для работы системы

### Выписать все CUD события и какие данные нужны для этих событий, которые необходимы для работы системы