from django.test import TestCase
from declarations.management.commands.generate_dedupe_pairs import RunDedupe
import os
import declarations.models as models
from declarations.management.commands.permalinks import TPermaLinksDB


def create_default_source_document():
    models.Section.objects.all().delete()
    models.Source_Document.objects.all().delete()
    models.Person.objects.all().delete()
    src_doc = models.Source_Document(id=1)
    src_doc.office_id = 1
    src_doc.save()
    return src_doc


class CreateNewPersonId(TestCase):

    def test(self):
        src_doc = create_default_source_document()

        models.Section(id=1, source_document=src_doc, person_name="Иванов Иван Иванович").save()
        models.Section(id=2, source_document=src_doc, person_name="Иванов И. И.").save()

        permalinks_path = os.path.join(os.path.dirname(__file__), "permalinks.dbm")
        p = TPermaLinksDB(permalinks_path)
        p.create_db()
        p.create_sql_sequences()
        p.close()
        run_dedupe = RunDedupe(None, None)
        run_dedupe.handle(None,
                          permanent_links_db=permalinks_path,
                          write_to_db=True,
                          fake_dedupe=True,
                          surname_bounds=',',
                          rebuild=True)

        self.assertEqual(models.Person.objects.count(), 1)
        person = models.Section.objects.get(id=1)
        self.assertEqual(person.person_name, "Иванов Иван Иванович")

        sec1 = models.Section.objects.get(id=1)
        self.assertEqual(sec1.person_id, 1)

        sec2 = models.Section.objects.get(id=2)
        self.assertEqual(sec2.person_id, 1)


class UseOldPersonId(TestCase):

    def test(self):
        src_doc = create_default_source_document()

        person = models.Person(id=2, declarator_person_id=1111, person_name="Иванов Иван Иванович")
        person.save()

        models.Section(id=1, source_document=src_doc, person_name="Иванов Иван Иванович", person=person).save()
        models.Section(id=2, source_document=src_doc, person_name="Иванов И. И.").save()

        person.refresh_from_db()

        permalinks_path = os.path.join(os.path.dirname(__file__), "permalinks.dbm")
        p = TPermaLinksDB(permalinks_path)
        p.create_db()
        p.save_next_primary_key_value(models.Person, 3)
        p.create_sql_sequences()
        p.close()

        run_dedupe = RunDedupe(None, None)
        run_dedupe.handle(None,
                          permanent_links_db=permalinks_path,
                          write_to_db=True,
                          fake_dedupe=True,
                          surname_bounds=',',
                          rebuild=True)

        sec1 = models.Section.objects.get(id=1)
        self.assertEqual(sec1.person_id, person.id)

        sec2 = models.Section.objects.get(id=2)
        self.assertEqual(sec2.person_id, person.id)


class RememberOldPersonId(TestCase):

    def test(self):
        src_doc = create_default_source_document()
        models.Section(id=1, source_document=src_doc, person_name="Иванов Иван Иванович").save()
        models.Section(id=2, source_document=src_doc, person_name="Иванов И. И.").save()

        permalinks_path = os.path.join(os.path.dirname(__file__), "permalinks.dbm")
        db = TPermaLinksDB(permalinks_path)
        db.create_db()
        person = models.Person(id=99)
        person.tmp_section_set = {str(1), str(2)}
        db.put_record_id(person)
        db.save_next_primary_key_value(models.Person, 100)
        db.create_sql_sequences()
        db.close()

        run_dedupe = RunDedupe(None, None)
        run_dedupe.handle(None,
                          permanent_links_db=permalinks_path,
                          write_to_db=True,
                          fake_dedupe=True,
                          surname_bounds=',',
                          rebuild=True)

        sec1 = models.Section.objects.get(id=1)
        self.assertEqual(sec1.person_id, person.id)

        sec2 = models.Section.objects.get(id=2)
        self.assertEqual(sec2.person_id, person.id)
