import argparse
import json
import re
import os

def parse_args():
    smart_parser_default =  os.path.join(
                    os.path.dirname(os.path.realpath(__file__)),
                    "../../src/bin/Release/netcoreapp3.1/smart_parser"
            )
    if os.path.sep == "\\":
        smart_parser_default += ".exe"

    parser = argparse.ArgumentParser()
    parser.add_argument("--source-file", dest='source_file', required=True)
    parser.add_argument("--txt-file", dest='txt_file', required=True)
    parser.add_argument("--output", dest='output', default=None)
    parser.add_argument("--smart-parser-binary",
                        dest='smart_parser_binary',
                        default=os.path.normpath(smart_parser_default))
    args = parser.parse_args()
    return args


def get_matches(match_object, result, name, max_count=10):
    if match_object is None:
        return False
    matches = list()
    first_offset = -1
    for x in match_object:
        if first_offset == -1:
            first_offset = x.start()
        matches.append(str(x))
        if len(matches) >= max_count:
            result[name] = {
                'matches': matches,
                "start": first_offset
            }
            break
    if len(matches) == 0:
        return False
    result[name] = {
        'matches': matches,
        "start": first_offset
    }
    return True


def find_person(input_text, result, name):
    regexp = "[А-Я]\w+ [А-Я]\w+ [А-Я]\w+((вич)|(ьич)|(кич)|(вна)|(чна))" # # Сокирко Алексей Викторович
    if get_matches(re.finditer(regexp, input_text), result, name):
        pass
    else:
        regexp = "[А-Я]\w+ [А-Я]\. *[А-Я]\."   # Сокирко А.В.
        get_matches(re.finditer(regexp, input_text), result, name)


def find_relatives(input_text, result, name):
    regexp = "супруга|(несовершеннолетний ребенок)|сын|дочь|(супруг\b)"
    get_matches(re.finditer(regexp, input_text), result, name)


def find_vehicles(input_text, result, name):
    regexp = r"\b(Opel|Ситроен|Мазда|Mazda|Пежо|Peageut|BMV|БМВ|Ford|Форд|Toyota|Тойота|KIA|ТАГАЗ|Шевроле|Chevrolet|Suzuki|Сузуки|Mercedes|Мерседес|Renault|Рено|Мицубиси|Rover|Ровер|Нисан|Nissan|Ауди|Audi|Вольво)\b"
    get_matches(re.finditer(regexp, input_text, re.IGNORECASE), result, name)


def find_vehicles_word(input_text, result, name):
    regexp = "транспорт|транспортных"
    get_matches(re.finditer(regexp, input_text, re.IGNORECASE), result, name)


def find_income(input_text, result, name):
    regexp = '[0-9]{6}'
    get_matches(re.finditer(regexp, input_text.replace(' ', ''), re.IGNORECASE), result, name)


def find_realty(input_text, result, name):
    regexp = "квартира|(земельный участок)|(жилое помещение)|комната|долевая|(з/ *участок)|(ж/ *дом)"
    get_matches(re.finditer(regexp, input_text, re.IGNORECASE), result, name)


def find_suname_word(input_text, result, name):
    regexp = "(фамилия)|(фио)|(ф.и.о.)"
    get_matches(re.finditer(regexp, input_text, re.IGNORECASE), result, name)


def find_header(input_text, result, name):
    regexps = [
        r"(Сведения о доходах)",
        r"(Сведения о расходах)",
        r"(Сведения об имущественном положении и доходах)",
        r"((Фамилия|ФИО).{1,200}Должность.{1,200}Перечень объектов.{1,200}транспортных)",
        r"(Сведения *,? предоставленные руководителями)",
        r"(Перечень объектов недвижимого имущества ?, принадлежащих)"
    ]

    regexp = '(' + "|".join(regexps) + ")"
    get_matches(re.finditer(regexp, input_text, re.IGNORECASE), result, name)


def find_other_document_types(input_text, result, name):
    words = list()
    for w in ['постановление', 'решение', 'доклад', 'протокол', 'план', 'указ', 'реестр', 'утверждена']:
        words.append('(' + " *".join(w) + ')')
    regexp = '(' + "|".join(words) + ")" + r"\b"
    get_matches(re.finditer(regexp, input_text, re.IGNORECASE), result, name)


def process_smart_parser_json(json_file):
    with open(json_file, "r", encoding="utf8") as inpf:
        smart_parser_json = json.load(inpf)
        people_count = len(smart_parser_json.get("persons", []))
    os.remove(json_file)
    return people_count


def get_smart_parser_result(smart_parser_binary, source_file):
    if smart_parser_binary == "none":
        return -1

    if not os.path.exists(smart_parser_binary):
        raise Exception("cannot find {}".format(smart_parser_binary))

    if source_file.endswith("pdf"):  # cannot process new pdf without conversion
        return 0

    cmd = "{} -skip-relative-orphan -skip-logging -adapter prod -fio-only {}".format(smart_parser_binary,
                                                                                           source_file)
    os.system(cmd)

    json_file = source_file + ".json"
    if os.path.exists(json_file):
        people_count = process_smart_parser_json(json_file)
    else:
        sheet_index = 0
        people_count = 0
        while True:
            json_file = "{}_{}.json".format(source_file, sheet_index)
            if not os.path.exists(json_file):
                break
            people_count += process_smart_parser_json(json_file)
            sheet_index += 1
    return people_count


if __name__ == "__main__":
    args = parse_args()
    with open(args.txt_file, "r", encoding="utf8", errors="ignore") as inpf:
        input_text = inpf.read().replace("\n", " ").replace("\r", " ").replace ('"', ' ').strip("\t \n\r")
        input_text = input_text.replace('*', '')  #footnotes
        input_text = ' '.join(input_text.split())
    result = {
        "result": "unknown_result",
        "smart_parser_person_count":  get_smart_parser_result(args.smart_parser_binary, args.source_file)
    }
    result["start_text"] = input_text[0:500]
    result["text_len"] = len(input_text)
    _, file_extension = os.path.splitext(args.source_file)
    if len (input_text) < 200:
        if file_extension in {".html", ".htm", ".docx", ".doc", ".xls", ".xlsx"}:
            if len(input_text) == 0:
                result["description"] = "file is too short" # jpeg in document
            else:
                result["result"] = "some_other_document_result"  # fast empty files, but not empty
        else:
            result["description"] = "file is too short"
    elif re.search(r"[аоиуяю]", input_text, re.IGNORECASE) is None: # no Russian vowels
        result["result"] = "unknown_result"
        result["description"] = "cannot find Russian chars, may be encoding problems"
    elif result['smart_parser_person_count'] > 0 and len(input_text) / result['smart_parser_person_count'] < 2048:
        result["result"] = "declaration_result"
    else:
        find_person(input_text, result, "person")
        find_relatives(input_text, result, "relative") #not used
        find_vehicles(input_text, result, "auto")
        find_vehicles_word(input_text, result, "transport_word")
        find_income(input_text, result, "income") #not used
        find_realty(input_text, result, "realty")
        find_header(input_text, result, "header")
        find_other_document_types(input_text, result, "other_document_type")
        find_suname_word(input_text, result, "surname_word")
        person_count = len(result.get('person', dict()).get('matches', list()))
        relative_count = len(result.get('relative', dict()).get('matches', list()))
        realty_count = len(result.get('realty', dict()).get('matches', list()))
        vehicle_count = len(result.get('auto', dict()).get('matches', list()))
        is_declaration = False
        if result.get('other_document_type') is not None and result['other_document_type']['start'] < 400:
            pass
        elif vehicle_count > 0:
            is_declaration = True
        elif result.get("surname_word", dict()).get("start", 1) == 0 and len(input_text) < 2000 and person_count > 0 and realty_count > 0:
            is_declaration = True
        elif result.get("header", dict()).get("start", 1) == 0:
            is_declaration = True
        elif realty_count > 5:
            is_declaration = True
        elif person_count > 0 and result.get("header") is not None:
            if person_count > 2 and result["header"]['start'] < result["person"]['start']:
                is_declaration = True
            else:
                if realty_count > 0:
                    is_declaration = True

        result["result"] = "declaration_result" if is_declaration else "some_other_document_result"

    with open (args.output, "w", encoding="utf8") as outf:
        outf.write( json.dumps(result, ensure_ascii=False, indent=4) )
