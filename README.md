# UnitySQLiteAsync

UnitySQLiteAsync is asynchronous SQLite-net support for Unity.

[sqlite-net](https://github.com/praeclarum/sqlite-net) is simple, powerful, cross-platform SQLite client and ORM for .NET. But sqlite-net's Asynchronous API based on Threading, it's heavy and not matched to Unity threading(single-thread). So I made Asynchronous API based on [UniTask](https://github.com/Cysharp/UniTask), which is more suitable for Unity.

Also Can use Synchronous API.

## Prerequisites
Required Unity after 2018.3


### Installing

[Download package](https://github.com/kdw9502/UnitySQLiteAsync/raw/master/UnitySQLiteAsync.unitypackage) and import to your Unity project. You don't nead to import test scripts in UnitySQLiteAsync folder.

Package contains [UniTask](https://github.com/Cysharp/UniTask), [sqlite-net](https://github.com/praeclarum/sqlite-net), [sqlite-net-extensions](https://bitbucket.org/twincoders/sqlite-net-extensions)

## Example
You can also find example in [sqlite-net Example](https://github.com/praeclarum/sqlite-net#example-time) and [Document](https://github.com/praeclarum/sqlite-net/wiki). you need to replace Task to UniTask, Task.Result to await UniTask.

To create and use new database, use path with Application.persistentDataPath.

To modify prepared database, insert database file in Assets/StreamingAssets and use path with Application.streamingAssetsPath

The library contains simple attributes that you can use to control the construction of tables
```c#
public class Customer
{
    [AutoIncrement, PrimaryKey]
    public int Id { get; set; }

    [MaxLength (64)]
    public string FirstName { get; set; }

    [MaxLength (64)]
    public string LastName { get; set; }

    [MaxLength (64), Indexed]
    public string Email { get; set; }
}
```
Insert row example
```c#
public async UniTask AddCustomerAsync(Customer customer)
{
    var databasePath = Application.persistentDataPath + databaseName;
    var db = new SQLiteAsyncConnection(databasePath);

    await db.InsertAsync(customer);
}
```
Get example
```c#
public async UniTask<Customer> GetCustomerAsync(int id)
{
    var databasePath = Application.persistentDataPath + databaseName;
    var db = new SQLiteAsyncConnection(databasePath);
    
    Customer customer = await db.GetAsync<Customer> (customer.Id);
    return customer;
}
```
Create(or Update new column) example
```c#
public async void Main()
{
    var databasePath = Application.persistentDataPath + databaseName;
    var db = new SQLiteAsyncConnection(databasePath);
    
    await db.CreateTableAsync<Customer> ();
    
    await AddCustomerAsync(new Customer());
    var customer = await GetCustomerAsync(0);
}
```

## Running the tests
Since UniTask runs only in play mode, use [Unity Test Runner](https://docs.unity3d.com/2019.1/Documentation/Manual/testing-editortestsrunner.html) with PlayMode. Unity Test Runner also help you to test in device.
![testRunner](https://user-images.githubusercontent.com/21076531/69316848-0276b200-0c7d-11ea-884f-f4bf43f99556.png)

Android (V30, API Level 29) passed all 195 tests.

iOS (iPad 7th gen, iOS 13.5.1) passed all 195 tests.


All the tests were imported from [sqlite-net](https://github.com/praeclarum/sqlite-net) and converted to the Unitask version.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
