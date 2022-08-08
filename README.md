# NullCheckRemover - JetBrains 2021 Summer test task
Задание - написать рослин анализатор для поиска различных проверок на null только для аргументов(!). Бонусом фиксер, который вырезает эти проверки.
## Анализатор
Реализован анализ на:
* Оператор ??
* Оператор ??=
* Оператор ?.
* Обычное сравнение через ==/!= с null или default
* Паттерн матчинг
  * null/not null
  * { }
  * В свитч экспрешне
* Свитч стейтмент

Не реализовано:
* ReferenceEquals (не успел, хотя делается быстро)
* Паттерн матчинг на object (`is object`). Не уверен, что так вообще кто-то делает, да и в целом, что это нужнго саппортить

## Фиксер
Реализован фикс для:
* Оператор ?? - остается только правая часть выражения
* Оператор ?. - остается выражение без ?
* Оператор ??= - в основном выражение полностью удаляется. Если возвращаемое значение оператора ??= используется, то левая часть выражения остается
* Сравнение через ==/!= - если выражение простое (без `&&`/`||`), то if/тернарный оператор инлайнятся. Если выражение сложное,
то в некоторых случаях выражение немного упрощается (`args == null || something`) -> (`something`). Если не получилось, то просто заменяется на `true`/`false`
* Свитч стейтмент - если свитч только с одним `case null`, то удаляется весь свитч. Если `case` несколько, то удаляется блок, принадлежащий `case null:`.
Если несколько `case` идут подряд к одному блоку с `case null:`, то удаляется только `case null`.

Не реализовано:
* Паттерн матчинг - сложно. Нельзя сделать по-простому, как в случае с `==`/`!=`, т.к. заменить на `true`/`false` нельзя

## Глобально
* Нужно сделать логирование и какие-то всплывающие уведомления. Сейчас и анализ, и фиксер завернут в `try-catch`, но ошибки просто проглатываются.
* У фиксера бывают проблемы с табуляцией.
* Анализ не работает на аргументы лямбд
* Дженерики по простым тестам вроде работают - но ничего не гарантирую
* Nullable Reference из C# 8 - не саппортится
* where T : not null тоже никак не влияет и не убирается
