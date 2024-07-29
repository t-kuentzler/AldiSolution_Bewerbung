using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;
using Shared.Repositories;
using Xunit;

namespace Shared.Tests.Repositories
{
    public class OrderRepositoryTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public OrderRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        // Damit bei jedem Test eine neue Memory-Datenbank verwendet wird
        private ApplicationDbContext CreateDbContext()
        {
            return new ApplicationDbContext(_options);
        }

        [Fact]
        public async Task CreateOrderAsync_AddsOrderToDatabase()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var orderRepository = new OrderRepository(dbContext);

            var order = new Order
            {
                Id = 1,
                Code = "Order1",
                Status = "Pending",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow,
                AldiCustomerNumber = "123456",
                EmailAddress = "test@example.com",
                Phone = "123456789",
                Language = "DE",
                OrderDeliveryArea = "A",
                Exported = false
            };

            // Act
            await orderRepository.CreateOrderAsync(order);

            // Assert
            var savedOrder = await dbContext.Order.FindAsync(order.Id);
            Assert.NotNull(savedOrder);
            Assert.Equal(order.Code, savedOrder.Code);
        }

        [Fact]
        public async Task CreateOrderAsync_ThrowsRepositoryException_OnFailure()
        {
            // Arrange
            var dbContextMock = new Mock<ApplicationDbContext>(_options);
            dbContextMock.Setup(db => db.SaveChangesAsync(default))
                .ThrowsAsync(new Exception("Test exception"));

            var orderRepository = new OrderRepository(dbContextMock.Object);
            var order = new Order
            {
                Id = 1,
                Code = "Order1",
                Status = "Pending",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow,
                AldiCustomerNumber = "123456",
                EmailAddress = "test@example.com",
                Phone = "123456789",
                Language = "DE",
                OrderDeliveryArea = "A",
                Exported = false
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RepositoryException>(() => orderRepository.CreateOrderAsync(order));

            // Assert
            Assert.Contains("Ein unerwarteter Fehler ist aufgetreten. OrderId:", exception.Message);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_UpdatesOrderStatus()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var orderRepository = new OrderRepository(dbContext);

            var order = new Order
            {
                Code = "Order1",
                Status = "Pending",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow,
                AldiCustomerNumber = "123456",
                EmailAddress = "test@example.com",
                Phone = "123456789",
                Language = "DE",
                OrderDeliveryArea = "A",
                Exported = false
            };
            dbContext.Order.Add(order);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await orderRepository.UpdateOrderStatusAsync("Order1", "Shipped");

            // Assert
            Assert.True(result);
            var updatedOrder = await dbContext.Order.FindAsync(order.Id);
            Assert.Equal("Shipped", updatedOrder.Status);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_ThrowsRepositoryException_OnFailure()
        {
            // Arrange
            var dbContextMock = new Mock<ApplicationDbContext>(_options);
            dbContextMock.Setup(db => db.SaveChangesAsync(default))
                .ThrowsAsync(new Exception("Test exception"));

            var orderRepository = new OrderRepository(dbContextMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RepositoryException>(() => orderRepository.UpdateOrderStatusAsync("Order1", "Shipped"));

            // Assert
            Assert.Contains("Ein unerwarteter Fehler ist aufgetreten. OrderCode:", exception.Message);
        }

        [Fact]
        public async Task GetOrderByOrderCodeAsync_ReturnsOrder_WhenOrderExists()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var orderRepository = new OrderRepository(dbContext);

            var order = new Order
            {
                Code = "Order1",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow,
                AldiCustomerNumber = "123456",
                EmailAddress = "test@example.com",
                Phone = "123456789",
                Language = "DE",
                OrderDeliveryArea = "A",
                Exported = false,
                Status = "InProgress"
            };
            dbContext.Order.Add(order);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await orderRepository.GetOrderByOrderCodeAsync("Order1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(order.Code, result.Code);
        }

        [Fact]
        public async Task GetOrderByOrderCodeAsync_ThrowsRepositoryException_OnFailure()
        {
            // Arrange
            var dbContextMock = new Mock<ApplicationDbContext>(_options);
            dbContextMock.Setup(db => db.SaveChangesAsync(default))
                .ThrowsAsync(new Exception("Test exception"));

            var orderRepository = new OrderRepository(dbContextMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RepositoryException>(() => orderRepository.GetOrderByOrderCodeAsync("Order1"));

            // Assert
            Assert.Contains("Ein unerwarteter Fehler ist aufgetreten. OrderCode:", exception.Message);
        }

        [Fact]
        public async Task GetOrderStatusByOrderCodeAsync_ReturnsStatus_WhenOrderExists()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var orderRepository = new OrderRepository(dbContext);

            var order = new Order
            {
                Code = "Order1",
                Status = "Pending",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow,
                AldiCustomerNumber = "123456",
                EmailAddress = "test@example.com",
                Phone = "123456789",
                Language = "DE",
                OrderDeliveryArea = "A",
                Exported = false
            };
            dbContext.Order.Add(order);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await orderRepository.GetOrderStatusByOrderCodeAsync("Order1");

            // Assert
            Assert.Equal("Pending", result);
        }

        [Fact]
        public async Task GetOrderStatusByOrderCodeAsync_ThrowsOrderNotFoundException_WhenOrderDoesNotExist()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var orderRepository = new OrderRepository(dbContext);

            // Act & Assert
            await Assert.ThrowsAsync<OrderNotFoundException>(() => orderRepository.GetOrderStatusByOrderCodeAsync("NonExistentCode"));
        }

        [Fact]
        public async Task GetOrderStatusByOrderCodeAsync_ThrowsRepositoryException_OnFailure()
        {
            // Arrange
            var dbContextMock = new Mock<ApplicationDbContext>(_options);
            dbContextMock.Setup(db => db.SaveChangesAsync(default))
                .ThrowsAsync(new Exception("Test exception"));

            var orderRepository = new OrderRepository(dbContextMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RepositoryException>(() => orderRepository.GetOrderStatusByOrderCodeAsync("Order1"));

            // Assert
            Assert.Contains("Ein unerwarteter Fehler ist aufgetreten. OrderCode:", exception.Message);
        }

        [Fact]
        public async Task UpdateOrderStatusByOrderCodeAsync_UpdatesOrderStatus()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var orderRepository = new OrderRepository(dbContext);

            var order = new Order
            {
                Code = "Order1",
                Status = "Pending",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow,
                AldiCustomerNumber = "123456",
                EmailAddress = "test@example.com",
                Phone = "123456789",
                Language = "DE",
                OrderDeliveryArea = "A",
                Exported = false
            };
            dbContext.Order.Add(order);
            await dbContext.SaveChangesAsync();

            // Act
            await orderRepository.UpdateOrderStatusByOrderCodeAsync("Order1", "Shipped");

            // Assert
            var updatedOrder = await dbContext.Order.FindAsync(order.Id);
            Assert.Equal("Shipped", updatedOrder.Status);
        }

        [Fact]
        public async Task UpdateOrderStatusByOrderCodeAsync_ThrowsRepositoryException_OnFailure()
        {
            // Arrange
            var dbContextMock = new Mock<ApplicationDbContext>(_options);
            dbContextMock.Setup(db => db.SaveChangesAsync(default))
                .ThrowsAsync(new Exception("Test exception"));

            var orderRepository = new OrderRepository(dbContextMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RepositoryException>(() => orderRepository.UpdateOrderStatusByOrderCodeAsync("Order1", "Shipped"));

            // Assert
            Assert.Contains("Ein unerwarteter Fehler ist aufgetreten. OrderCode:", exception.Message);
        }

        [Fact]
        public async Task GetOrdersWithStatusAsync_ReturnsOrdersWithSpecifiedStatus()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var orderRepository = new OrderRepository(dbContext);

            var orders = new List<Order>
            {
                new Order { Code = "Order1", Status = "Pending", Created = DateTime.UtcNow, Modified = DateTime.UtcNow, AldiCustomerNumber = "123456", EmailAddress = "test@example.com", Phone = "123456789", Language = "DE", OrderDeliveryArea = "A", Exported = false },
                new Order { Code = "Order2", Status = "Shipped", Created = DateTime.UtcNow, Modified = DateTime.UtcNow, AldiCustomerNumber = "123456", EmailAddress = "test@example.com", Phone = "123456789", Language = "DE", OrderDeliveryArea = "A", Exported = false },
                new Order { Code = "Order3", Status = "Pending", Created = DateTime.UtcNow, Modified = DateTime.UtcNow, AldiCustomerNumber = "123456", EmailAddress = "test@example.com", Phone = "123456789", Language = "DE", OrderDeliveryArea = "A", Exported = false }
            };
            dbContext.Order.AddRange(orders);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await orderRepository.GetOrdersWithStatusAsync("Pending");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, o => Assert.Equal("Pending", o.Status));
        }

        [Fact]
        public async Task GetOrdersWithStatusAsync_ThrowsRepositoryException_OnFailure()
        {
            // Arrange
            var dbContextMock = new Mock<ApplicationDbContext>(_options);
            dbContextMock.Setup(db => db.SaveChangesAsync(default))
                .ThrowsAsync(new Exception("Test exception"));

            var orderRepository = new OrderRepository(dbContextMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RepositoryException>(() => orderRepository.GetOrdersWithStatusAsync("Pending"));

            // Assert
            Assert.Contains("Ein unerwarteter Fehler ist aufgetreten. Status:", exception.Message);
        }

        [Fact]
        public async Task UpdateOrderStatusByIdAsync_UpdatesOrderStatus()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var orderRepository = new OrderRepository(dbContext);

            var order = new Order { Id = 1, Code = "123", Status = "Pending", Created = DateTime.UtcNow, Modified = DateTime.UtcNow, AldiCustomerNumber = "123456", EmailAddress = "test@example.com", Phone = "123456789", Language = "DE", OrderDeliveryArea = "A", Exported = false };
            dbContext.Order.Add(order);
            await dbContext.SaveChangesAsync();

            // Act
            await orderRepository.UpdateOrderStatusByIdAsync(1, "Shipped");

            // Assert
            var updatedOrder = await dbContext.Order.FindAsync(order.Id);
            Assert.Equal("Shipped", updatedOrder.Status);
        }

        [Fact]
        public async Task UpdateOrderStatusByIdAsync_ThrowsRepositoryException_OnFailure()
        {
            // Arrange
            var dbContextMock = new Mock<ApplicationDbContext>(_options);
            dbContextMock.Setup(db => db.SaveChangesAsync(default))
                .ThrowsAsync(new Exception("Test exception"));

            var orderRepository = new OrderRepository(dbContextMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RepositoryException>(() => orderRepository.UpdateOrderStatusByIdAsync(1, "Shipped"));
            
            // Assert
            Assert.Contains("Ein unerwarteter Fehler ist aufgetreten beim aktualisieren des Status für die Order", exception.Message);
        }


        [Fact]
        public async Task GetOrderByIdAsync_ReturnsOrder_WhenOrderExists()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var orderRepository = new OrderRepository(dbContext);

            var order = new Order { Id = 1, Code = "123", Created = DateTime.UtcNow, Modified = DateTime.UtcNow, AldiCustomerNumber = "123456", EmailAddress = "test@example.com", Phone = "123456789", Language = "DE", OrderDeliveryArea = "A", Exported = false, Status = "InProgress" };
            dbContext.Order.Add(order);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await orderRepository.GetOrderByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(order.Id, result.Id);
        }

        [Fact]
        public async Task GetOrderByIdAsync_ThrowsRepositoryException_OnFailure()
        {
            // Arrange
            var dbContextMock = new Mock<ApplicationDbContext>(_options);
            dbContextMock.Setup(db => db.SaveChangesAsync(default))
                .ThrowsAsync(new Exception("Test exception"));

            var orderRepository = new OrderRepository(dbContextMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RepositoryException>(() => orderRepository.GetOrderByIdAsync(1));

            // Assert
            Assert.Contains("Ein unerwarteter Fehler ist aufgetreten. OrderId:", exception.Message);
        }

        [Fact]
        public async Task SearchOrdersAsync_ReturnsOrdersMatchingSearchTermAndStatus()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var orderRepository = new OrderRepository(dbContext);

            var orders = new List<Order>
            {
                new Order { Code = "Order1", Status = "Pending", Created = DateTime.UtcNow, Modified = DateTime.UtcNow, AldiCustomerNumber = "123456", EmailAddress = "test@example.com", Phone = "123456789", Language = "DE", OrderDeliveryArea = "A", Exported = false },
                new Order { Code = "Order2", Status = "Shipped", Created = DateTime.UtcNow, Modified = DateTime.UtcNow, AldiCustomerNumber = "123456", EmailAddress = "test@example.com", Phone = "123456789", Language = "DE", OrderDeliveryArea = "A", Exported = false },
                new Order { Code = "Order3", Status = "Pending", Created = DateTime.UtcNow, Modified = DateTime.UtcNow, AldiCustomerNumber = "123456", EmailAddress = "test@example.com", Phone = "123456789", Language = "DE", OrderDeliveryArea = "A", Exported = false }
            };
            dbContext.Order.AddRange(orders);
            await dbContext.SaveChangesAsync();
            var searchTerm = new SearchTerm { value = "Order" };

            // Act
            var result = await orderRepository.SearchOrdersAsync(searchTerm, "Pending");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, o => Assert.Equal("Pending", o.Status));
        }

        [Fact]
        public async Task SearchOrdersAsync_ThrowsRepositoryException_OnFailure()
        {
            // Arrange
            var dbContextMock = new Mock<ApplicationDbContext>(_options);
            dbContextMock.Setup(db => db.SaveChangesAsync(default))
                .ThrowsAsync(new Exception("Test exception"));

            var orderRepository = new OrderRepository(dbContextMock.Object);
            var searchTerm = new SearchTerm { value = "Order" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RepositoryException>(() => orderRepository.SearchOrdersAsync(searchTerm, "Pending"));

            // Assert
            Assert.Contains("Ein unerwarteter Fehler ist aufgetreten beim Suchen von Bestellungen. Suchbegriff:", exception.Message);
        }

        [Fact]
        public async Task UpdateOrderEntryAsync_UpdatesOrderEntry()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var orderRepository = new OrderRepository(dbContext);

            var orderEntry = new OrderEntry { Id = 1, ProductName = "Product1", AldiProductCode = "123", VendorProductCode = "345" };
            dbContext.OrderEntry.Add(orderEntry);
            await dbContext.SaveChangesAsync();
            orderEntry.ProductName = "UpdatedProduct";

            // Act
            await orderRepository.UpdateOrderEntryAsync(orderEntry);

            // Assert
            var updatedOrderEntry = await dbContext.OrderEntry.FindAsync(orderEntry.Id);
            Assert.Equal("UpdatedProduct", updatedOrderEntry.ProductName);
        }

        [Fact]
        public async Task UpdateOrderEntryAsync_ThrowsRepositoryException_OnFailure()
        {
            // Arrange
            var dbContextMock = new Mock<ApplicationDbContext>(_options);
            dbContextMock.Setup(db => db.SaveChangesAsync(default))
                .ThrowsAsync(new Exception("Test exception"));

            var orderRepository = new OrderRepository(dbContextMock.Object);
            var orderEntry = new OrderEntry { Id = 1, ProductName = "Product1", AldiProductCode = "123", VendorProductCode = "345" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RepositoryException>(() => orderRepository.UpdateOrderEntryAsync(orderEntry));

            // Assert
            Assert.Contains("Ein unerwarteter Fehler ist aufgetreten. OrderEntry:", exception.Message);
        }

        [Fact]
        public async Task UpdateOrderAsync_UpdatesOrder()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var orderRepository = new OrderRepository(dbContext);

            var order = new Order
            {
                Id = 1,
                Code = "Order1",
                AldiCustomerNumber = "123456",
                EmailAddress = "test@example.com",
                Phone = "123456789",
                Language = "DE",
                OrderDeliveryArea = "A",
                Exported = false,
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow,
                Status = "Pending"
            };
            dbContext.Order.Add(order);
            await dbContext.SaveChangesAsync();

            // Act
            order.EmailAddress = "newemail@example.com";
            await orderRepository.UpdateOrderAsync(order);

            // Assert
            var updatedOrder = await dbContext.Order.FindAsync(order.Id);
            Assert.Equal("newemail@example.com", updatedOrder.EmailAddress);
        }

        [Fact]
        public async Task UpdateOrderAsync_ThrowsRepositoryException_OnFailure()
        {
            // Arrange
            var dbContextMock = new Mock<ApplicationDbContext>(_options);
            dbContextMock.Setup(db => db.SaveChangesAsync(default))
                .ThrowsAsync(new Exception("Test exception"));

            var orderRepository = new OrderRepository(dbContextMock.Object);
            var order = new Order
            {
                Id = 1,
                Code = "Order1",
                AldiCustomerNumber = "123456",
                EmailAddress = "test@example.com",
                Phone = "123456789",
                Language = "DE",
                OrderDeliveryArea = "A",
                Exported = false,
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow,
                Status = "Pending"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RepositoryException>(() => orderRepository.UpdateOrderAsync(order));

            // Assert
            Assert.Contains("Ein unerwarteter Fehler ist aufgetreten. Order:", exception.Message);
        }
    }
}
