import sys
import json
import xml.etree.ElementTree as etree
from pathlib import Path, PurePath

PO_ID = 'msgid "%s"'
PO_STR = 'msgstr "%s"'
PO_LINE = '"%s\\n"'
PO_LANG = 'Language: %s'


def load_xml(path):
    print('[XML]', path)
    tree = etree.parse(path)
    root = tree.getroot()

    result = {}
    for language in root:
        keys = {}
        for entry in language.findall("Dict/Entry"):
            key = entry.attrib['key']
            keys[key] = entry.text

        name = language.attrib['key']
        name = 'en' if not name else name
        result[name] = keys

    return result


def load_headers(path):
    with open(path, mode='rt', encoding="utf-8") as headers_file:
        text = headers_file.read()
        return text.split('\n\n')


def write_json(path, data):
    print('[JSON]', path)
    with open(path, mode='wt', encoding='utf8') as json_file:
        json.dump(data, json_file, indent=4, ensure_ascii=False)


def write_po(path, data, headers):
    for language, messages in data.items():
        po_lang = str(path) % language
        print('[PO]', po_lang)

        with open(po_lang, mode='wt', encoding='utf8') as po_file:
            for item in headers:
                search = PO_LANG % language
                if search in item:
                    header = item + "\n"
                    break

            print(header, file=po_file)
            for tag, msg in messages.items():
                fmt = msg.replace("\r", "\\r")
                fmt = fmt.replace("\n", "\\n")

                print(PO_ID % tag, file=po_file)
                print(PO_STR % fmt, file=po_file)
                print(file=po_file)


def write_pot(path, messages):
    print('[POT]', path)
    with open(path, mode='wt', encoding='utf8') as pot_file:
        print(PO_ID % "", file=pot_file)
        print(PO_STR % "", file=pot_file)
        print(file=pot_file)

        for tag in messages.keys():
            print(PO_ID % tag, file=pot_file)
            print(PO_STR % "", file=pot_file)
            print(file=pot_file)


#orig_path = Path("D:\\Zeffyr\\tools\\statictext.xml").resolve()
orig_path = Path(sys.argv[1]).resolve()
json_path = orig_path.with_suffix(".json")
po_path = orig_path.with_suffix(".%s.po")
pot_path = orig_path.with_suffix(".pot")
pos_path = PurePath(orig_path.parent, Path("headers.po"))

parsed = load_xml(orig_path)
headers = load_headers(pos_path)

write_json(json_path, parsed)
write_pot(pot_path, parsed['en'])
write_po(po_path, parsed, headers)