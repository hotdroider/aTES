# aTES
Awesome Task Exchange System (by popugs, for popugs)

Начальный черновой проект

![Initial Diagram](https://github.com/hotdroider/aTES/blob/main/aTES%20initial.drawio.svg)

## Выбор сервисов

На каждую область ответственности попробуем выделить свой сервис

* Authorization Service - сюда попуг показывает клюв и получает свои клеймсы, если честно авторизацию никогда не строил сам с нуля
* Task Tracker Service - сервис ответственный только за состав самих тасок, создание, распределение, список для каждого попуга. 
* Accounting Service - сервис ответственный за денюжки и биллинг для каждого юзера. В данном плане этот же сервис по идее закрывает день.
* Analytics Service - сервис занимающийся только сбором требуемой статистики 

## Коммуникации

Запросы UI за данными (список тасок, биллинги, аналитика) - синхронно.
Запрос на перетасовку тасок или уведомление о комплите - тоже синхронно.
Тут мб использовать http или gRPC...

Уведомления о изменении в мире тасок или денежках - асинхронно через message broker, 
но на данном этапе кажется что очереди тут "чтобы было". Тем не менее уже какой-никакой фейловер, 
если какой нибудь сервис полностью отвалится то потом доделает работу по очереди, лишь бы в ней остались нужные события.

## Что делать в случае проблем

На первый взгляд - успех любой операции (перетасовка тасок, комплит таски, обработка сообщения например для занесения записи в биллинг) - сохранить стейт 
внутри конкретного сервиса и успешно сообщить об этом миру (записать инфу об этом в очередь).
В случае провала сети или БД стейт роллбечим, если попуг жал кнопку - попуга огорчаем, если сообщение не смогли обработать - не коммитим.

## Спорные места

* Очень хочется шарить данные по привычке, аля намотать вышеупомянутые сервисы можно на одну БД. Но это монолитный монолит, 
не важно что БД можно реплицировать или делать RO реплики для аналитики - всегда упремся в базу рано или поздно.
Проблема обратная - не понятно как данными оперировать, как обеспечивается целостность.
Делать что-то синхронно уже заведомо кажется неправильным, но как например получить список всех юзеров в системе при перетасовке тасок? Либо синхронно ходить в сервис авторизации либо надо было слушать его события о новых регистрациях и хранить юзера у себя внутри. Как правильнее - непонятно.
На данный момент получается что таски например можно хранить и в тасксервисе и в аккаунтинге, но использовать специфические для сервиса поля, лишь бы совпадали ключи внутри системы. Это догадки.
* Надо думать над синхронизацией и порядком обработки сообщений, тут вероятно могут помочь таймстампы в событиях и фиксация времени обработки в сервисах.
* При радикальных изменениях в ТЗ (структура данных, новые фичи) - помоги нам Попуг.
