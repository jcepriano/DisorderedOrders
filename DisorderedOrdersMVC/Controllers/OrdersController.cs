using DisorderedOrdersMVC.DataAccess;
using DisorderedOrdersMVC.Models;
using DisorderedOrdersMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DisorderedOrdersMVC.Controllers
{
    public class OrdersController : Controller
    {
        private readonly DisorderedOrdersContext _context;
        private readonly IPaymentProcessor _paymentProcessor;

        public OrdersController(DisorderedOrdersContext context, IPaymentProcessor paymentProcessor)
        {
            _context = context;
            _paymentProcessor = paymentProcessor;
        }

        public IActionResult New(int customerId)
        {
            var products = _context.Products.Where(p => p.StockQuantity > 0);
            ViewData["CustomerId"] = customerId;

            return View(products);
        }

        public interface IPaymentProcessor
        {

            void ProcessPayment(int amount, string paymentType);
        }

        public class BitcoinProcessor : IPaymentProcessor
        {
            public void ProcessPayment(int amount, string paymentType)
            {
                if (paymentType == "bitcoin")
                {
                    new BitcoinProcessor();
                }
            }
        }

        public class PayPalProcessor : IPaymentProcessor
        {
            public void ProcessPayment(int amount, string paymentType)
            {
                if (paymentType == "paypal")
                {
                    new PayPalProcessor();
                }
            }
        }

        public class CreditCardProcessor : IPaymentProcessor
        {
            public void ProcessPayment(int amount, string paymentType)
            {
                new CreditCardProcessor();
            }
        }

        private Order CreateOrder(IFormCollection collection, Customer customer)
        {
            var order = new Order() { Customer = customer };

            for (var i = 1; i < collection.Count - 1; i++)
            {
                var kvp = collection.ToList()[i];
                if (kvp.Value != "0")
                {
                    var product = _context.Products.Where(p => p.Name == kvp.Key).First();
                    var orderItem = new OrderItem() { Item = product, Quantity = Convert.ToInt32(kvp.Value) };
                    order.Items.Add(orderItem);
                }
            }
            return order;
        }

        [HttpPost]
        [Route("/orders")]
        public IActionResult Create(IFormCollection collection, string paymentType)
        {
            // create order
            int customerId = Convert.ToInt16(collection["CustomerId"]);
            Customer customer = _context.Customers.Find(customerId);
            var order = CreateOrder(collection, customer);

            // verify stock available
            foreach (var orderItem in order.Items)
            {
                if (!orderItem.Item.InStock(orderItem.Quantity))
                {
                    orderItem.Quantity = orderItem.Item.StockQuantity;
                }

                orderItem.Item.DecreaseStock(orderItem.Quantity);
            }

            // calculate total price
            var total = 0;
            foreach (var orderItem in order.Items)
            {
                var itemPrice = orderItem.Item.Price * orderItem.Quantity;
                total += itemPrice;
            }

            // process payment
            //IPaymentProcessor processor;
            //if (paymentType == "bitcoin")
            //{
            //    processor = new BitcoinProcessor();
            //}
            //else if (paymentType == "paypal")
            //{
            //    processor = new PayPalProcessor();
            //}
            //else
            //{
            //    processor = new CreditCardProcessor();
            //}



            _paymentProcessor.ProcessPayment(total, paymentType);

            _context.Orders.Add(order);
            _context.SaveChanges();

            return RedirectToAction("Show", new { id = order.Id });
        }

        [Route("/orders/{id:int}")]
        public IActionResult Show(int id)
        {
            var order = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Item)
                .Where(o => o.Id == id).First();

            var total = 0;
            foreach (var orderItem in order.Items)
            {
                var itemPrice = orderItem.Item.Price * orderItem.Quantity;
                total += itemPrice;
            }
            ViewData["total"] = total;

            return View(order);
        }
    }
}
