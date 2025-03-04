﻿using Billings.Domain.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace Billings.Application.Models
{
    public class BillingResponse
    {
        public BillingResponse(Billing billing)
        {
            Id = billing.Id;
            Amount = billing.Amount;
            Cpf = billing.Cpf.ToString().PadLeft(11, '0');
            DueDate = billing.DueDate.ToString();
        }
        [Required] public Guid Id { get; set; }
        [Required] public string Cpf { get; set; }
        [Required] public string DueDate { get; set; }
        [Required] public double Amount { get; set; }
    }
}
