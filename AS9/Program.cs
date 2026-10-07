using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SmartDeliverySystem
{
    // ---------------------------------------------------------------
    // Supporting types (from Assignment 03 - adjust to match yours)
    // ---------------------------------------------------------------
    public enum ShipmentStatus
    {
        Ready,
        OutForDelivery,
        Delivered
    }

    public class DeliveryAddress
    {
        public string Street { get; }
        public string City { get; }
        public string Country { get; }

        public DeliveryAddress(string street, string city, string country)
        {
            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("City cannot be empty.");
            if (string.IsNullOrWhiteSpace(country))
                throw new ArgumentException("Country cannot be empty.");

            Street = street;
            City = city;
            Country = country;
        }
    }

    // ---------------------------------------------------------------
    // Interfaces (contracts)
    // ---------------------------------------------------------------
    public interface ITrackable
    {
        string GetTrackingStatus();
    }

    public interface IInsurable
    {
        decimal CalculateInsurance();
    }

    // ---------------------------------------------------------------
    // 1 + 2) Abstract base class
    // ---------------------------------------------------------------
    public abstract class Shipment
    {
        private string _trackingCode;
        private string _description;
        private decimal _weight;
        private decimal _deliveryFee;
        private DeliveryAddress _destination;

        public string TrackingCode
        {
            get => _trackingCode;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Tracking code cannot be empty.");
                _trackingCode = value;
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Description cannot be empty.");
                _description = value;
            }
        }

        public decimal Weight
        {
            get => _weight;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Weight must be greater than zero.");
                _weight = value;
            }
        }

        public decimal DeliveryFee
        {
            get => _deliveryFee;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Delivery fee cannot be negative.");
                _deliveryFee = value;
            }
        }

        public DeliveryAddress Destination
        {
            get => _destination;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value), "Destination is required.");
                _destination = value;
            }
        }

        public ShipmentStatus Status { get; set; } = ShipmentStatus.Ready;

        protected Shipment(string trackingCode, string description, decimal weight,
                           decimal deliveryFee, DeliveryAddress destination)
        {
            TrackingCode = trackingCode;
            Description = description;
            Weight = weight;
            DeliveryFee = deliveryFee;
            Destination = destination;
        }

        // Abstract members: every shipment type MUST provide its own version.
        public abstract decimal EstimatedCost { get; }
        public abstract void PrintShipment();

        // Shared helper used by the concrete classes' GetTrackingStatus().
        protected string BuildTrackingMessage()
        {
            switch (Status)
            {
                case ShipmentStatus.Ready:
                    return $"Shipment {TrackingCode} is Ready.";
                case ShipmentStatus.OutForDelivery:
                    return $"Shipment {TrackingCode} is Out for Delivery.";
                case ShipmentStatus.Delivered:
                    return $"Shipment {TrackingCode} has been Delivered.";
                default:
                    return $"Shipment {TrackingCode} has an unknown status.";
            }
        }
    }

    // ---------------------------------------------------------------
    // 3) Concrete shipment types
    // ---------------------------------------------------------------
    public class StandardShipment : Shipment, ITrackable, IInsurable
    {
        private const decimal RatePerKg = 15m;

        public StandardShipment(string trackingCode, string description, decimal weight,
                                decimal deliveryFee, DeliveryAddress destination)
            : base(trackingCode, description, weight, deliveryFee, destination) { }

        // Estimated cost = delivery fee + (weight * rate per kg)
        public override decimal EstimatedCost => DeliveryFee + Weight * RatePerKg;

        public override void PrintShipment()
        {
            Console.WriteLine("Standard Shipment");
            Console.WriteLine($"Tracking Code : {TrackingCode}");
            Console.WriteLine($"Description   : {Description}");
            Console.WriteLine($"Estimated Cost: {EstimatedCost} EGP");
        }

        public string GetTrackingStatus() => BuildTrackingMessage();

        public decimal CalculateInsurance() => EstimatedCost * 0.05m;
    }

    public class ExpressShipment : Shipment, ITrackable, IInsurable
    {
        public decimal ExtraFee { get; }

        public ExpressShipment(string trackingCode, string description, decimal weight,
                               decimal deliveryFee, DeliveryAddress destination, decimal extraFee)
            : base(trackingCode, description, weight, deliveryFee, destination)
        {
            if (extraFee < 0)
                throw new ArgumentException("Extra fee cannot be negative.");
            ExtraFee = extraFee;
        }

        // Estimated cost = delivery fee + express extra fee
        public override decimal EstimatedCost => DeliveryFee + ExtraFee;

        public override void PrintShipment()
        {
            Console.WriteLine("Express Shipment");
            Console.WriteLine($"Tracking Code : {TrackingCode}");
            Console.WriteLine($"Extra Fee     : {ExtraFee} EGP");
            Console.WriteLine($"Estimated Cost: {EstimatedCost} EGP");
        }

        public string GetTrackingStatus() => BuildTrackingMessage();

        public decimal CalculateInsurance() => EstimatedCost * 0.08m;
    }

    public class InternationalShipment : Shipment, ITrackable, IInsurable
    {
        public decimal CustomsFee { get; }

        public InternationalShipment(string trackingCode, string description, decimal weight,
                                     decimal deliveryFee, DeliveryAddress destination, decimal customsFee)
            : base(trackingCode, description, weight, deliveryFee, destination)
        {
            if (customsFee < 0)
                throw new ArgumentException("Customs fee cannot be negative.");
            CustomsFee = customsFee;
        }

        // Estimated cost = delivery fee + customs fee
        public override decimal EstimatedCost => DeliveryFee + CustomsFee;

        public override void PrintShipment()
        {
            Console.WriteLine("International Shipment");
            Console.WriteLine($"Tracking Code        : {TrackingCode}");
            Console.WriteLine($"Destination Country  : {Destination.Country}");
            Console.WriteLine($"Estimated Cost       : {EstimatedCost} EGP");
        }

        public string GetTrackingStatus() => BuildTrackingMessage();

        public decimal CalculateInsurance() => EstimatedCost * 0.12m;
    }

    // ---------------------------------------------------------------
    // 6) DeliveryReport - works with INTERFACES, not concrete classes
    // ---------------------------------------------------------------
    public class DeliveryReport
    {
        public void PrintShipment(ITrackable shipment)
        {
            Console.WriteLine(shipment.GetTrackingStatus());
        }

        public void PrintInsurance(IInsurable shipment)
        {
            // "StandardShipment" -> "Standard Shipment"
            string name = Regex.Replace(shipment.GetType().Name, "(?<!^)([A-Z])", " $1");
            Console.WriteLine($"{name} Insurance : {shipment.CalculateInsurance().ToString("F2")} EGP");
        }
    }

    // ---------------------------------------------------------------
    // 7) DeliveryCenter (reused from Assignment 03 + new method)
    // ---------------------------------------------------------------
    public class DeliveryCenter
    {
        private readonly List<Shipment> _shipments = new List<Shipment>();

        public IReadOnlyList<Shipment> Shipments => _shipments;

        public void AddShipment(Shipment shipment)
        {
            if (shipment == null)
                throw new ArgumentNullException(nameof(shipment));
            _shipments.Add(shipment);
        }

        public void PrintAllShipments()
        {
            for (int i = 0; i < _shipments.Count; i++)
            {
                _shipments[i].PrintShipment();      // runtime polymorphism
                if (i < _shipments.Count - 1)
                {
                    Console.WriteLine();
                    Console.WriteLine("------------------------------------------");
                    Console.WriteLine();
                }
            }
        }

        // New in Assignment 04
        public void PrintTrackingStatuses()
        {
            foreach (Shipment shipment in _shipments)
            {
                // Shipment itself is not ITrackable, so we check at runtime.
                if (shipment is ITrackable trackable)
                    Console.WriteLine(trackable.GetTrackingStatus());
            }
        }
    }

    // ---------------------------------------------------------------
    // 8) Main
    // ---------------------------------------------------------------
    public class Program
    {
        public static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            // a, b, c) create the three shipments
            var standard = new StandardShipment(
                "SH001", "Laptop", 3m, 50m,
                new DeliveryAddress("12 Nile St", "Tanta", "Egypt"));
            standard.Status = ShipmentStatus.Ready;

            var express = new ExpressShipment(
                "SH002", "Documents", 1m, 70m,
                new DeliveryAddress("5 Tahrir Sq", "Cairo", "Egypt"), 30m);
            express.Status = ShipmentStatus.OutForDelivery;

            var international = new InternationalShipment(
                "SH003", "Camera", 2m, 200m,
                new DeliveryAddress("Hauptstrasse 1", "Berlin", "Germany"), 60m);
            international.Status = ShipmentStatus.Delivered;

            // d) add all shipments to the DeliveryCenter
            var center = new DeliveryCenter();
            center.AddShipment(standard);
            center.AddShipment(express);
            center.AddShipment(international);

            var report = new DeliveryReport();

            // e) print all shipment details
            Console.WriteLine("==========================================");
            Console.WriteLine("Delivery Center");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            center.PrintAllShipments();

            // f) print the tracking status of every shipment
            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine("Tracking Status");
            Console.WriteLine();
            center.PrintTrackingStatuses();

            // g) print the insurance cost of every shipment
            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine("Insurance");
            Console.WriteLine();
            report.PrintInsurance(standard);
            report.PrintInsurance(express);
            report.PrintInsurance(international);

            // h) ITrackable[] array
            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine("ITrackable[] Array");
            Console.WriteLine();
            ITrackable[] trackables = { standard, express, international };
            foreach (ITrackable item in trackables)
                report.PrintShipment(item);

            // i) IInsurable[] array
            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine("IInsurable[] Array");
            Console.WriteLine();
            IInsurable[] insurables = { standard, express, international };
            foreach (IInsurable item in insurables)
                report.PrintInsurance(item);

            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine("Interface Polymorphism Demonstrated Successfully.");
        }
    }
}