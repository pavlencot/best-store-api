using BestStoreApi.Models;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BestStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private ApplicationDbContext context;

        public ContactsController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpGet("subjects")]
        public IActionResult GetSubjects()
        {
            var listSubjects = context.Subjects.ToList();

            return Ok(listSubjects);
        }

        [HttpGet]
        public IActionResult GetContacts(int? page)
        {
            if(page == null || page < 1)
            {
                page = 1;
            }

            int pageSize = 3;
            int totalPages = 0;

            decimal count = context.Contacts.Count();
            totalPages = (int) Math.Ceiling(count / pageSize);

            var contacts = context.Contacts
                .Include(context => context.Subject)
                .OrderByDescending(c => c.Id)
                .Skip((int)(page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new
            {
                Contacts = contacts,
                TotalPages = totalPages,
                PageSize = pageSize,
                Page = page
            };

            return Ok(response);
        }

        [HttpGet("{id}")]
        public IActionResult GetContact(int id)
        {
            var contact = context.Contacts.Include(context => context.Subject).FirstOrDefault(c => c.Id == id);

            if(contact == null)
            {
                return NotFound();
            }

            return Ok(contact); 
        }

        [HttpPost]
        public IActionResult CreateContact(ContactDto contactDto)
        {
            var subject = context.Subjects.Find(contactDto.SubjectId);

            if(subject == null)
            {
                ModelState.AddModelError("Subject", "Please select a valid subject");
                return BadRequest(ModelState);
            }

            Contact contact = new Contact()
            {
                Firstname = contactDto.Firstname,
                Lastname = contactDto.Lastname,
                Email = contactDto.Email,
                Phone = contactDto.Phone ?? "",
                Subject = subject,
                Message = contactDto.Message,
                CreatedAt = DateTime.Now,
            };

            context.Contacts.Add(contact);
            context.SaveChanges();

            return Ok(contact);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateContact(int id, ContactDto contactDto) 
        {
            var subject = context.Subjects.Find(contactDto.SubjectId);

            if (subject == null)
            {
                ModelState.AddModelError("Subject", "Please select a valid subject");
                return BadRequest(ModelState);
            }

            var contact = context.Contacts.Find(id);

            if(contact == null)
            {
                return NotFound(contactDto);
            }

            contact.Firstname = contactDto.Firstname;
            contact.Lastname = contactDto.Lastname;
            contact.Email = contactDto.Email;
            contact.Phone = contactDto.Phone ?? "";
            contact.Subject = subject;
            contact.Message = contactDto.Message;

            context.SaveChanges();
            return Ok(contact);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteContact(int id)
        {
            //method 1
            /* var contact = context.Contacts.Find(id);
             if(contact == null)
             {
                 return NotFound();
             }

             context.Contacts.Remove(contact);
             context.SaveChanges();

             return Ok();*/

            //method 2
            try
            {
                var contact = new Contact() { Id = id, Subject = new Subject() };
                context.Contacts.Remove(contact);
                context.SaveChanges();
            }
            catch (Exception)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
