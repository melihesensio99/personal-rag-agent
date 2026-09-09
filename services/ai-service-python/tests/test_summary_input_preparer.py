from xml.etree import ElementTree

import pytest

from app.services.summary_input_preparer import SummaryInputPreparer
from app.providers.extractors.pmc_article_extractor import PmcArticleExtractor


def test_preserves_paragraphs_and_numbers():
    text = '## Methods\n\n  120 participants;  12 weeks.\n\n## Results\n\nChange: 2.5 kg (95% CI 1.2–3.8).'
    result = SummaryInputPreparer.prepare(text)
    assert '\n\n' in result
    assert '120 participants; 12 weeks.' in result
    assert '2.5 kg (95% CI 1.2–3.8)' in result


def test_long_article_keeps_evidence_and_omits_reference_section():
    text = '\n\n'.join([
        '# Introduction', 'Background sentence. ' * 2500,
        '# Methods', '120 participants followed for 12 weeks. ' * 500,
        '# Results', 'Measured improvement was 2.5 kg. ' * 500,
        '# Limitations', 'Small sample limits generalizability. ' * 500,
        '# References', 'UNWANTED CITATION ' * 2000,
    ])
    result = SummaryInputPreparer.prepare(text)
    assert len(result) <= 40000
    for heading in ('Methods', 'Results', 'Limitations'):
        assert f'## {heading}' in result
    assert '120 participants' in result
    assert '2.5 kg' in result
    assert 'Small sample' in result
    assert 'UNWANTED CITATION' not in result
    assert 'Selected excerpts' in result


def test_reference_word_in_prose_is_not_removed():
    text = 'The references support this claim.\n\nThe results are preliminary.'
    assert SummaryInputPreparer.prepare(text) == text


def test_cleanup_resumes_at_next_section():
    result = SummaryInputPreparer.prepare('# References\nUnwanted citation\n# Results\nImportant finding.')
    assert 'Unwanted' not in result
    assert 'Important finding.' in result


def test_unstructured_fallback_is_bounded_and_covers_document():
    text = 'START ' + 'a ' * 10000 + 'MIDDLE ' + 'b ' * 10000 + 'END'
    result = SummaryInputPreparer.prepare(text)
    assert len(result) <= 40000
    assert 'START' in result and 'MIDDLE' in result and 'END' in result


def test_many_headings_remain_bounded():
    text = '\n'.join(f'# Section {i}\n' + 'text ' * 100 for i in range(1000))
    assert len(SummaryInputPreparer.prepare(text)) <= 40000


def test_empty_input_rejected():
    with pytest.raises(ValueError):
        SummaryInputPreparer.prepare(' \n\t')


def test_pmc_structure_preserves_inline_text_and_excludes_references():
    root = ElementTree.fromstring('''<article><abstract><p>Research question.</p></abstract>
    <body><sec><title>Methods</title><p>Studied <italic>120</italic> adults.</p></sec>
    <sec><title>Results</title><p>Improved by 2.5 kg.</p></sec>
    <ref-list><ref>Unwanted citation.</ref></ref-list></body></article>''')
    result = PmcArticleExtractor._article_text(root)
    assert '## Abstract' in result and '## Methods' in result and '## Results' in result
    assert 'Studied 120 adults.' in result
    assert 'Unwanted citation' not in result
