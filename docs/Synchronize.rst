Synchronization types
=================================

You have one main method to launch a synchronization, with several optional parameters:

.. code-block:: csharp

	SynchronizeAsync();
	SynchronizeAsync(IProgress<ProgressArgs> progress);
	SynchronizeAsync(CancellationToken cancellationToken);
	SynchronizeAsync(SyncType syncType);
	SynchronizeAsync(SyncType syncType, CancellationToken cancellationToken);


| You can use the ``CancellationToken`` object whenever you want to rollback an "*in progress*" synchronization.
| And since we have an async synchronization, you can pass an ``IProgress<ProgressArgs>`` object to have feedback during the sync process.

.. note:: The progression system is explained in the next chapter `Progress <Progression.html>`_ 


let's see now a straightforward sample illustrating the use of the ``SyncType`` argument.

.. hint:: You will find the sample used for this chapter, here : `SyncType sample <https://github.com/Mimetis/Dotmim.Sync/tree/master/Samples/SyncType>`_ 

.. code-block:: csharp

	SqlSyncProvider serverProvider = new SqlSyncProvider(GetDatabaseConnectionString("AdventureWorks"));
	SqlSyncProvider clientProvider = new SqlSyncProvider(GetDatabaseConnectionString("Client"));

	var setup = new SyncSetup("ProductCategory", "ProductModel", "Product", "Address", "Customer", 
		"CustomerAddress", "SalesOrderHeader", "SalesOrderDetail");

	SyncAgent agent = new SyncAgent(clientProvider, serverProvider);

	var syncContext = await agent.SynchronizeAsync(setup);

	Console.WriteLine(syncContext);

Here is the result, after the **first initial** synchronization:

.. code-block:: bash

	Synchronization done.
		Total changes  uploaded: 0
		Total changes  downloaded: 2752
		Total changes  applied: 2752
		Total resolved conflicts: 0
		Total duration :0:0:4.720

As you can see, the client has downloaded 2752 lines from the server.   

Obviously if we made a new sync, without making any changes neither on the server nor the client, the result will be :

.. code-block:: csharp

	SqlSyncProvider serverProvider = new SqlSyncProvider(GetDatabaseConnectionString("AdventureWorks"));
	SqlSyncProvider clientProvider = new SqlSyncProvider(GetDatabaseConnectionString("Client"));

	SyncAgent agent = new SyncAgent(clientProvider, serverProvider);

	var syncContext = await agent.SynchronizeAsync();

	Console.WriteLine(syncContext);

.. note:: Since you've made a first sync before, the setup is already saved in the databases. So far, no need to pass the argument anymore now.

.. code-block:: bash

	Synchronization done.
		Total changes  uploaded: 0
		Total changes  downloaded: 0
		Total changes  applied: 0
		Total resolved conflicts: 0
		Total duration :0:0:0.382

Ok make sense !

SyncType
^^^^^^^^^^^^

| The ``SyncType`` enumeration allows you to **reinitialize** a client database (already synchronized or not).  
| For various reason, you may want to re-download the whole database schema and rows from the server (bug, out of sync, and so on ...)

``SyncType`` is mainly an enumeration used when calling the ``SynchronizeAsync()`` method:

.. code-block:: csharp

	public enum SyncType
	{
		/// <summary>
		/// Normal synchronization
		/// </summary>
		Normal,

		/// <summary>
		/// Reinitialize the whole sync database, applying all rows from the server to the client
		/// </summary>
		Reinitialize,
		
		/// <summary>
		/// Reinitialize the whole sync database, applying all rows from the server to the client, 
		/// after tried a client upload
		/// </summary>
		ReinitializeWithUpload
	}


* ``SyncType.Normal``: Default value, represents a normal sync process.
* ``SyncType.Reinitialize``: Marks the client to be resynchronized. Be careful, any changes on the client will be overwritten by this value.
* ``SyncType.ReinitializeWithUpload``: Like *Reinitialize* this value will launch a process to resynchronize the whole client database, except that the client will *try* to send its local changes before making the resync process.

From the sample we saw before, here is the different behaviors with each ``SyncType`` enumeration value:  

First of all, for demo purpose, we are updating a row on the **client**:

.. code-block:: sql

	-- initial value is 'The Bike Store'
	UPDATE Client.dbo.Customer SET CompanyName='The New Bike Store' WHERE CustomerId = 1 


SyncType.Normal
--------------------

Let's see what happens, now that we have updated a row on the client side, with a *normal* sync:

.. code-block:: csharp

	SqlSyncProvider serverProvider = new SqlSyncProvider(GetDatabaseConnectionString("AdventureWorks"));
	SqlSyncProvider clientProvider = new SqlSyncProvider(GetDatabaseConnectionString("Client"));

	var syncContext = await agent.SynchronizeAsync();

	Console.WriteLine(syncContext);

.. code-block:: bash

	Synchronization done.
			Total changes  uploaded: 1
			Total changes  downloaded: 0
			Total changes  applied: 0
			Total resolved conflicts: 0
			Total duration :0:0:1.382

The default behavior is what we were waiting for: Uploading the modified row to the server.

SyncType.Reinitialize
-------------------------

The ``SyncType.Reinitialize`` mode will **reinitialize** the whole client database.

Every rows on the client will be deleted and downloaded again from the server, even if some of them are not synced correctly.

Use this mode with caution, since you could lost some "*out of sync client*" rows.

.. code-block:: csharp

	SqlSyncProvider serverProvider = new SqlSyncProvider(GetDatabaseConnectionString("AdventureWorks"));
	SqlSyncProvider clientProvider = new SqlSyncProvider(GetDatabaseConnectionString("Client"));

	var syncContext = await agent.SynchronizeAsync(SyncType.Reinitialize);

	Console.WriteLine(syncContext);

.. code-block:: bash

	Synchronization done.
			Total changes  uploaded: 0
			Total changes  downloaded: 2752
			Total changes  applied: 2752
			Total resolved conflicts: 0
			Total duration :0:0:1.872

As you can see, the ``SyncType.Reinitialize`` value has marked the client database to be fully resynchronized.  

The modified row on the client has not been sent to the server and then has been restored to the initial value sent by the server row.


SyncType.ReinitializeWithUpload
-----------------------------------

``ReinitializeWithUpload`` will do the same job as ``Reinitialize`` except it will send any changes available from the client, before making the reinitialize phase.


.. code-block:: csharp

	SqlSyncProvider serverProvider = new SqlSyncProvider(GetDatabaseConnectionString("AdventureWorks"));
	SqlSyncProvider clientProvider = new SqlSyncProvider(GetDatabaseConnectionString("Client"));

	var syncResult = await agent.SynchronizeAsync(SyncType.ReinitializeWithUpload);

	Console.WriteLine(syncResult);

.. code-block:: bash

	Synchronization done.
			Total changes  uploaded: 1
			Total changes  downloaded: 2752
			Total changes  applied: 2752
			Total resolved conflicts: 0
			Total duration :0:0:1.923

In this case, as you can see, the ``SyncType.ReinitializeWithUpload`` value has marked the client database to be fully resynchronized, but the edited row has been sent correctly to the server.  


Forcing operations on the client from server side 
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

.. warning:: This part covers some concept explained later in the next chapters:

			* Progression : `Using interceptors <Progression.html#interceptor-t>`_.
			* HTTP architecture : `Using ASP.Net Web API <Web.html>`_ 


| This technic applies if you do not have access to the client machine, allowing you to *force* operations from the client side.
| It could be useful to *override* a normal synchronization, for example, with a reinitialization for a particular client, from the server side.

.. note:: Forcing a reinitialization from the server is a good practice if you have an **HTTP** architecture.

Here are the operation action you can use to force the client in a particular situation:

.. code-block:: csharp

 public enum SyncOperation
 {
     /// <summary>
     /// Normal synchronization
     /// </summary>
     Normal = 0,

     /// <summary>
     /// Reinitialize the whole sync database, applying all rows from the server to the client
     /// </summary>
     Reinitialize = 1,
     
     /// <summary>
     /// Reinitialize the whole sync database, 
		 /// applying all rows from the server to the client, after trying a client upload
     /// </summary>
     ReinitializeWithUpload = 2,

     /// <summary>
     /// Drop all the sync metadatas even tracking tables and scope infos and make a full sync again
     /// </summary>
     DropAllAndSync = 4,

     /// <summary>
     /// Drop all the sync metadatas even tracking tables and scope infos and exit
     /// </summary>
     DropAllAndExit = 8,

     /// <summary>
     /// Deprovision stored procedures & triggers and sync again
     /// </summary>
     DeprovisionAndSync = 16,
 }


.. hint:: Use the client scope id to identify the current client trying to sync.


.. code-block:: csharp

	[HttpPost]
	public async Task Post()
	{
		// Get the current scope name
		var scopeName = this.HttpContext.GetScopeName();
		
		// Get the current client scope id
		var clientScopeId = this.HttpContext.GetClientScopeId();

		// override sync type to force a reinitialization from a particular client
		if (clientScopeId == OneParticularClientScopeIdToReinitialize)
		{
			webServerAgentRemoteOrchestrator.OnGettingOperation(operationArgs=>
			{
					// this operation will be applied for the current sync
					operationArgs.Operation = SyncOperation.Reinitialize; 
			});
		}

		// handle request
		await webServerAgent.HandleRequestAsync(this.HttpContext);
	}

SyncDirection
^^^^^^^^^^^^^^^^^^^^

| The `SyncType` enumeration allows you to synchronize **all** the tables.  
| Another way to synchronize your tables is to set a direction on each of them, through the `SyncDirection` enumeration. 
| This options is not global to all the tables, but should be set on each table.

You can specify three types of direction: **Bidirectional**, **UploadOnly** or **DownloadOnly**.

You can use the ``SyncDirection`` enumeration for each table in the ``SyncSetup`` object.

.. code-block:: csharp
	public enum SyncDirection
	{
		Bidirectional = 1,
		DownloadOnly = 2,
		UploadOnly = 3
	}

.. note:: ``Bidirectional`` is the default value for all tables added.

Since, we need to specify the direction on each table, the ``SyncDirection`` option is available on each ``SetupTable``:

.. code-block:: csharp

	var syncSetup = new SyncSetup("SalesLT.ProductCategory", "SalesLT.ProductModel", "SalesLT.Product",
			"SalesLT.Address", "SalesLT.Customer", "SalesLT.CustomerAddress");
	
	syncSetup.Tables["Customer"].SyncDirection = SyncDirection.DownloadOnly;
	syncSetup.Tables["CustomerAddress"].SyncDirection = SyncDirection.DownloadOnly;
	syncSetup.Tables["Address"].SyncDirection = SyncDirection.DownloadOnly;

	var agent = new SyncAgent(clientProvider, serverProvider);


SyncDirection.Bidirectional
---------------------------------

This mode is the default one. Both server and client will upload and download their rows. 

Using this mode, all your tables are fully synchronized with the server.

SyncDirection.DownloadOnly
---------------------------------

This mode allows you to specify some tables to be only downloaded from the server to the client.

Using this mode, your server will not receive any rows from any clients, on the configured tables with the download only option.

SyncDirection.UploadOnly
---------------------------------

This mode allows you to specify some tables to be uploaded from the client to the server only.

Using this mode, your server will not send any rows to any clients, but clients will sent their own modified rows to the server. 

