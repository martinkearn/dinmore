﻿using dinmore.api.Interfaces;
using dinmore.api.Models;
using Dinmore.Api.Models;
using Microsoft.Extensions.Options;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace dinmore.api.Repositories
{

    public class StoreRepository : IStoreRepository
    {
        private readonly AppSettings _appSettings;

        public StoreRepository(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public async Task StoreDevice(Device device)
        {
            var storageAccount = CloudStorageAccount.Parse(_appSettings.TableStorageConnectionString);

            var blobClient = storageAccount.CreateCloudTableClient();

            var table = blobClient.GetTableReference(_appSettings.StoreDeviceContainerName);

            await table.CreateIfNotExistsAsync();

            DeviceStorageTableEntity deviceStorageTableEntity = new DeviceStorageTableEntity(device.Id.ToString(), _appSettings.StoreDevicePartitionKey);
            deviceStorageTableEntity.DeviceLabel = device.DeviceLabel;
            deviceStorageTableEntity.Exhibit = device.Exhibit;
            deviceStorageTableEntity.Venue = device.Venue;

            TableOperation insertOperation = TableOperation.Insert(deviceStorageTableEntity);

            await table.ExecuteAsync(insertOperation);
        }

        public async Task DeleteDevice(string deviceId)
        {
            var storageAccount = CloudStorageAccount.Parse(_appSettings.TableStorageConnectionString);

            var tableClient = storageAccount.CreateCloudTableClient();
            
            var table = tableClient.GetTableReference(_appSettings.StoreDeviceContainerName);

            // Create a retrieve operation that expects a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<DeviceStorageTableEntity>(_appSettings.StoreDevicePartitionKey, deviceId);

            // Execute the operation.
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

            // Assign the result to a CustomerEntity.
            var deleteEntity = (DeviceStorageTableEntity)retrievedResult.Result;


            // Create the Delete TableOperation.
            if (deleteEntity != null)
            {
                // Create the Delete TableOperation.
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                await table.ExecuteAsync(deleteOperation);
            }

        }

        public async Task<Device> GetDevice(string deviceId)
        {
            var storageAccount = CloudStorageAccount.Parse(_appSettings.TableStorageConnectionString);

            var blobClient = storageAccount.CreateCloudTableClient();

            var table = blobClient.GetTableReference(_appSettings.StoreDeviceContainerName);

            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<DeviceStorageTableEntity>(_appSettings.StoreDevicePartitionKey, deviceId);

            // Execute the retrieve operation.
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

            if (retrievedResult.Result != null)
            {
                // get the result and create a new device from the data
                var deviceResult = (DeviceStorageTableEntity)retrievedResult.Result;

                var device = new Device()
                {
                    Id = new Guid(deviceResult.RowKey),
                    Exhibit = deviceResult.Exhibit,
                    DeviceLabel = deviceResult.DeviceLabel,
                    Venue = deviceResult.Venue
                };

                return device;
            }
            else
            {
                return null;
            }
        }

        public async Task<IEnumerable<Device>> GetDevices()
        {
            var storageAccount = CloudStorageAccount.Parse(_appSettings.TableStorageConnectionString);

            var blobClient = storageAccount.CreateCloudTableClient();

            var table = blobClient.GetTableReference(_appSettings.StoreDeviceContainerName);

            TableContinuationToken token = null;

            var entities = new List<DeviceStorageTableEntity>();
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(new TableQuery<DeviceStorageTableEntity>(), token);
                entities.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);

            // create list of device objects from the storage entities
            var devices = new List<Device>();
            foreach (var deviceStorageTableEntity in entities)
            {
                var device = new Device()
                {
                    Id = new Guid(deviceStorageTableEntity.RowKey),
                    Exhibit = deviceStorageTableEntity.Exhibit,
                    DeviceLabel = deviceStorageTableEntity.DeviceLabel,
                    Venue = deviceStorageTableEntity.Venue
                };

                devices.Add(device);
            }

            return devices;
        }

        public async Task StorePatrons(List<Patron> patrons)
        {
            var storageAccount = CloudStorageAccount.Parse(_appSettings.TableStorageConnectionString);

            var blobClient = storageAccount.CreateCloudTableClient();

            var table = blobClient.GetTableReference(_appSettings.StorePatronContainerName);

            await table.CreateIfNotExistsAsync();

            //insert an entity (row) per patron
            foreach (var patron in patrons)
            {
                var persistedFaceId = patron.PersistedFaceId;
                var sightingId = Guid.NewGuid().ToString(); //This is a unique ID for the sighting

                PatronStorageTableEntity patronStorageTableEntity = new PatronStorageTableEntity(persistedFaceId, sightingId);
                patronStorageTableEntity.Device = patron.Device;
                patronStorageTableEntity.Exhibit = patron.Exhibit;
                patronStorageTableEntity.Gender = patron.FaceAttributes.gender;
                patronStorageTableEntity.Age = Math.Round(patron.FaceAttributes.age,0);
                patronStorageTableEntity.PrimaryEmotion = patron.PrimaryEmotion;
                patronStorageTableEntity.TimeOfSighting = (DateTime)patron.Time;
                patronStorageTableEntity.Smile = patron.FaceAttributes.smile;
                patronStorageTableEntity.Glasses = patron.FaceAttributes.glasses;
                patronStorageTableEntity.FaceMatchConfidence = (double)patron.FaceMatchConfidence;

                TableOperation insertOperation = TableOperation.Insert(patronStorageTableEntity);

                await table.ExecuteAsync(insertOperation);
            }
        }



    }
}
