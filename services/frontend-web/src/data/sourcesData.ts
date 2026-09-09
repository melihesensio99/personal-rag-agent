import { SourceItem } from '../types';

export const INITIAL_SOURCES: SourceItem[] = [
  {
    id: 'karpathy-llm-101',
    type: 'youtube',
    title: 'Andrej Karpathy — Büyük Dil Modellerinin İnşası (LLM 101)',
    originalUrl: 'https://youtube.com/watch?v=zjkBMFHnJ_g',
    duration: '24:18',
    reliability: 98.4,
    synthesisNumber: '#ARC-2024-098',
    category: 'YAPAY ZEKA MİMARİSİ & NÖRAL TEMSİL',
    heroImage:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuBzEsrDI1mX6Z5DwuGqM1shsvwkMetB6dQapgDxb1Qo4CfXlmAJdk8mprLXqHJztPsYbEwHNNe3wwq15nPg2hKwtnL6Fc5XAmX55bgr4O9_kGuSwOS0YFWU0rZMpBK3MtKBFY4HHNRhMwHUsJcQnJK_uMa8n5pfYuO386HbmWX4Y2mk6ce290C0tRfp3X2kyN0RKiBYfvLB5Xy5La16fiAh0MMEiHZcvOwlNTQCToAZu6g3o9JZK4DpSw',
    author: {
      name: 'Andrej Karpathy',
      role: 'Eski Tesla Otopilot & OpenAI Kurucu Ortağı',
      avatarUrl:
        'https://lh3.googleusercontent.com/aida-public/AB6AXuAiR4NQDKerrq0eM_EopZGyQgmWkV6hGPi1zMvbWJUjd2xzXbtHU1L5aYwmdAKgFU-bDnroo_a-0lac8sgMYsM_h3-xC7l4AgJVd2w7pSyiYOv_JmYhSlMndr3IQYTraEB5Oqbl9sjVGJfVTP1dDUn151sGmdOP42W81ATn6UVBjqaWnOl_VNzwhtGPDZmt0Dmy9rBTOEGI_Le1hiAtts-iagAYBnh2LVjsTOE4rt4gaRY285QhI8_uXg',
    },
    executiveSummary: [
      'Karpathy, modern Büyük Dil Modellerini (LLM) basit birer metin tamamlama mekanizmasından öte, internet ölçeğindeki insan bilgisinin kayıplı bir sıkıştırma algoritması olarak konumlandırıyor. Ön-eğitim (pre-training) evresi, milyarlarca parametrenin metin içi ardışık örüntüleri öğrenmesiyle temel dünya modelini inşa eder; ancak bu aşamada model doğrudan soru-cevap protokolüne veya ahlaki sınırlara duyarlı değildir.',
      'İkinci evre olan İnce Ayar (Supervised Fine-Tuning - SFT) ve Takviyeli Öğrenme (RLHF), ham sıkıştırma ağırlıklarını birer bilişsel asistana dönüştürme zanaatıdır. Karpathy burada temel bir ödünleşimi vurguluyor: Aşırı kısıtlayıcı hizalama (alignment), modelin yaratıcı problem çözme ve mantıksal çıkarsama yeteneklerini törpüleyebilirken; yetersiz hizalama olgusal sapma ve güvenilirlik risklerini tırmandırmaktadır.',
      "Gelecek projeksiyonunda LLM'ler birer statik kütüphaneden ziyade, işletim sistemlerinin merkezi işlem birimi (CPU) gibi görev yapacaktır. Harici hafıza modülleri, arama API'leri ve anlamsal vektör dizinleriyle eşleşen hibrit mimariler, saf parametrik bellek kısıtlarını aşan yeni nesil araştırma ajanlarının temel omurgasını teşkil etmektedir.",
    ],
    findings: [
      {
        id: 'f1',
        phase: '01 / PRE-TRAIN',
        timestamp: '[04:12]',
        timeSeconds: 252,
        title: 'Kayıplı Zip Dosyası Olarak Model',
        description:
          "LLM'ler tüm web'in terabaytlarca ağırlığındaki ham dokümanlarını gigabayt seviyesindeki ağırlık matrislerine sıkıştırır; dolayısıyla olgusal hatırlama daima olasılıksaldır.",
        confidence: 'Güven Derecesi: Yüksek (%99)',
      },
      {
        id: 'f2',
        phase: '02 / TOKENIZATION',
        timestamp: '[09:30]',
        timeSeconds: 570,
        title: 'Tokenizasyon Sapmaları',
        description:
          'Kelimelerin alt-parçalara bölünüş biçimi, modellerin basit aritmetikte tökezlemesinin ve İngilizce dışı dillerde hesaplama verimsizliği yaşamasının temel kaynağıdır.',
        confidence: 'Güven Derecesi: Matematiksel Doğrulama',
      },
      {
        id: 'f3',
        phase: '03 / RLHF',
        timestamp: '[14:45]',
        timeSeconds: 885,
        title: 'Hizalama Vergi Problemi',
        description:
          'İnsan geri bildirimli pekiştirmeli öğrenme, güvenlik sağlarken keşifsel entropiyi daraltır. Model bilmediğini kabul etmek yerine bazen aşırı temkinli sessizliğe bürünür.',
        confidence: 'Güven Derecesi: Eleştirel Hipotez',
      },
      {
        id: 'f4',
        phase: '04 / FUTURE OS',
        timestamp: '[21:10]',
        timeSeconds: 1270,
        title: 'LLM İşletim Sistemi',
        description:
          'Geleceğin bilgi istasyonlarında LLM çekirdek işlemci, bağlam penceresi RAM, vektör veritabanları ise kalıcı depolama birimi rolünü üstlenecektir.',
        confidence: 'Güven Derecesi: Vizyoner Sentez',
      },
    ],
    qaPairs: [
      {
        id: 'qa1',
        question: "Karpathy'e göre LLM'ler neden matematikte zorlanıyor?",
        timeAgo: '2 dk önce',
        answer:
          'Ana neden Byte Pair Encoding (BPE) tabanlı tokenizasyondur. Sayılar sabit basamaklar yerine değişken token blokları olarak parçalandığı için, model karakter-seviyesi algoritmik toplamayı içselleştirmekte zorlanır.',
        citation: 'Transkript [09:30 - 11:15]',
        citationTimestamp: '09:30',
      },
      {
        id: 'qa2',
        question: 'Ön-eğitim ve SFT arasındaki maliyet/hesaplama farkı nedir?',
        timeAgo: '14 dk önce',
        answer:
          'Ön-eğitim (pre-training) sürecinde binlerce GPU aylarca çalışarak toplam bütçenin %99’unu tüketir. SFT evresi ise çok daha az sayıda (on binlerce) yüksek kaliteli diyalog verisiyle birkaç gün veya saat içinde tamamlanır.',
        citation: 'Transkript [12:05 - 13:40]',
        citationTimestamp: '12:05',
      },
    ],
    transcript: [
      {
        time: '00:15',
        seconds: 15,
        speaker: 'Andrej Karpathy',
        text: "Herkese merhaba. Bu oturumda büyük dil modellerinin (LLM) baştan sona nasıl inşa edildiğini, pre-training evresinden çıkarım (inference) ve ajan mimarilerine kadar adım adım ele alacağız.",
      },
      {
        time: '04:12',
        seconds: 252,
        speaker: 'Andrej Karpathy',
        text: "İlk olarak pre-training evresini düşünelim. Elimizde terabaytlarca internet dokümanı var. Bunu bir nevi kayıplı zip algoritması gibi düşünebilirsiniz. Trilyonlarca kelime 140 milyar parametreye sıkıştırılıyor.",
        highlighted: true,
      },
      {
        time: '09:30',
        seconds: 570,
        speaker: 'Andrej Karpathy',
        text: "Pek çok kişinin şaşırdığı konu tokenizasyon. Modeller harfleri tek tek görmez. Byte Pair Encoding (BPE) kullanırlar. '127 + 349' yazdığınızda bu sayılar rastgele token parçalarına bölünür, bu da modelin ilkokul aritmetiğinde bile tökezlemesine yol açabilir.",
        highlighted: true,
      },
      {
        time: '14:45',
        seconds: 885,
        speaker: 'Andrej Karpathy',
        text: "Reinforcement Learning from Human Feedback (RLHF) yaptığımızda modele 'hizalama vergisi' (alignment tax) ödetiyoruz. Model hem daha zararsız hale geliyor hem de özgün çıkarım potansiyelinin bir kısmını baskılayabiliyor.",
        highlighted: true,
      },
      {
        time: '21:10',
        seconds: 1270,
        speaker: 'Andrej Karpathy',
        text: "Nihai resimde LLM'leri bir sohbet robotu olarak değil, yeni bir tür işletim sisteminin CPU'su olarak görmeliyiz. RAM onun context window'u, hard drive ise anlamsal vektör veri tabanlarıdır.",
        highlighted: true,
      },
    ],
    chunks: [
      {
        id: 'chk-01',
        text: 'Pre-training evresi: Web crawler verilerinin filtrelenmesi, dedup algoritmaları ve LLaMA mimarisi üzerinde next-token prediction hedef fonksiyonu optimizasyonu.',
        tokens: 512,
        dimension: 1536,
        similarity: 0.94,
        category: 'Mimari / Pre-training',
      },
      {
        id: 'chk-02',
        text: 'Tokenizasyon ve Byte Pair Encoding: Karakter kümesi genişliği, UTF-8 byte dizilimleri, sayısal token bölünmeleri ve çok dilli verimsizlik analizi.',
        tokens: 480,
        dimension: 1536,
        similarity: 0.91,
        category: 'Tokenizasyon / BPE',
      },
      {
        id: 'chk-03',
        text: 'Hizalama ve RLHF: Tercih modellemesi (Reward Model), PPO optimizasyonu, DPO yaklaşımları ve aşırı reddetme (over-refusal) dinamikleri.',
        tokens: 530,
        dimension: 1536,
        similarity: 0.88,
        category: 'Hizalama / Güvenlik',
      },
      {
        id: 'chk-04',
        text: 'Ajan işletim sistemi: Tool use (fonksiyon çağırma), web gezintisi, hafıza konsolidasyonu ve RAG mimarileri ile hibrit bilişsel döngüler.',
        tokens: 495,
        dimension: 1536,
        similarity: 0.96,
        category: 'İşletim Sistemi / Ajanlar',
      },
    ],
    telemetry: {
      chunksCount: 48,
      referencedNamesCount: 14,
      wordsCount: '3.8k',
      vectorDimensions: 1536,
      matchPercentage: 100,
    },
    tags: ['Yapay Zeka', 'LLM', 'Pre-training', 'Tokenizasyon', 'RLHF'],
    dateAdded: '2024-11-20',
  },
  {
    id: 'lecun-world-models',
    type: 'youtube',
    title: 'Yann LeCun — JEPA ve Otonom Makine Zekasının Geleceği',
    originalUrl: 'https://youtube.com/watch?v=DocxK7X_zlc',
    duration: '38:40',
    reliability: 97.8,
    synthesisNumber: '#ARC-2024-104',
    category: 'DÜNYA MODELLERİ & ÖZ-DENETİMLİ ÖĞRENME',
    heroImage:
      'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?q=80&w=1200&auto=format&fit=crop',
    author: {
      name: 'Yann LeCun',
      role: 'Meta Baş Yapay Zeka Bilim İnsanı & NYU Profesörü',
      avatarUrl:
        'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?q=80&w=200&auto=format&fit=crop',
    },
    executiveSummary: [
      'Yann LeCun, saf otoregresif LLM’lerin (next-token prediction) genel yapay zekaya (AGI) giden doğru yol olmadığını, piksel veya token seviyesindeki ayrıntıları tahmin etmeye çalışmanın gereksiz bir hesaplama israfı yarattığını savunuyor.',
      'Önerdiği Joint Embedding Predictive Architecture (JEPA), temsilleri doğrudan soyut latent uzayda tahmin eder. Tıpkı bir bebeğin yerçekimi veya nesne sürekliliğini dünyayı gözlemleyerek öğrenmesi gibi, JEPA modelleri de fiziksel sağduyuyu (common sense) içselleştirebilir.',
      'Geleceğin otonom bilişsel sistemleri; Algı (Perception), Dünya Modeli (World Model), Maliyet/Hedef Fonksiyonu (Intrinsic Cost) ve Planlayıcı (Actor) bileşenlerinden oluşan hiyerarşik bir kontrol döngüsüyle inşa edilmelidir.',
    ],
    findings: [
      {
        id: 'lf1',
        phase: '01 / PARADİGMA',
        timestamp: '[06:18]',
        timeSeconds: 378,
        title: 'Otoregresyon Kısıtı',
        description:
          'Her yeni token tahminindeki küçük olasılıksal hatalar birikerek üstel sapmaya ve mantıksal halüsinasyona yol açar.',
        confidence: 'Güven Derecesi: Yüksek (%98)',
      },
      {
        id: 'lf2',
        phase: '02 / JEPA',
        timestamp: '[18:40]',
        timeSeconds: 1120,
        title: 'Soyut Uzayda Tahmin',
        description:
          'Ayrıntılı piksel üretmek yerine, görüntünün anlamsal özellik vektörlerindeki değişimleri tahmin etmek çok daha dayanıklıdır.',
        confidence: 'Güven Derecesi: Deneysel Doğrulama',
      },
      {
        id: 'lf3',
        phase: '03 / SAĞDUYU',
        timestamp: '[28:15]',
        timeSeconds: 1695,
        title: 'Fiziksel Dünya Modeli',
        description:
          '10 yaşındaki bir çocuk internetteki tüm metinleri okumamıştır ancak birkaç saniyelik fiziksel etkileşimle dünyanın temel kurallarını kavrar.',
        confidence: 'Güven Derecesi: Bilişsel Teori',
      },
    ],
    qaPairs: [
      {
        id: 'lqa1',
        question: "LeCun'a göre JEPA'nın LLM'lerden temel üstünlüğü nedir?",
        timeAgo: '1 saat önce',
        answer:
          'JEPA generative değil discriminative/latent tahmine dayanır. Belirsizlik ve gürültüyü soyutlayıp yalnızca kritik eylem ve durum geçişlerini modeller.',
        citation: 'Transkript [18:40 - 21:00]',
        citationTimestamp: '18:40',
      },
    ],
    transcript: [
      {
        time: '06:18',
        seconds: 378,
        speaker: 'Yann LeCun',
        text: 'Otoregresif dil modelleri etkileyici metinler üretiyor ama dünyayı anlamıyorlar. Hata payı her adımda katlanarak büyüyor.',
        highlighted: true,
      },
      {
        time: '18:40',
        seconds: 1120,
        speaker: 'Yann LeCun',
        text: "JEPA mimarisiyle hedefimiz, gereksiz arka plan detaylarını atıp nesnelerin hareket doğrultusunu soyut vektör uzayında öngörmektir.",
        highlighted: true,
      },
    ],
    chunks: [
      {
        id: 'lchk-01',
        text: 'JEPA Mimarisi: Context encoder, Target encoder ve Latent predictor matrisleri arasındaki enerji tabanlı optimizasyon dinamikleri.',
        tokens: 512,
        dimension: 1536,
        similarity: 0.93,
        category: 'Latent Temsiller',
      },
    ],
    telemetry: {
      chunksCount: 62,
      referencedNamesCount: 19,
      wordsCount: '5.2k',
      vectorDimensions: 1536,
      matchPercentage: 99,
    },
    tags: ['Dünya Modelleri', 'JEPA', 'Öz-Denetimli Öğrenme', 'Meta AI'],
    dateAdded: '2024-11-22',
  },
  {
    id: 'sutskever-agi-alignment',
    type: 'youtube',
    title: 'Ilya Sutskever — Yapay Genel Zekaya Doğru ve Süper Hizalama',
    originalUrl: 'https://youtube.com/watch?v=1u4bY8X9vYk',
    duration: '31:12',
    reliability: 99.1,
    synthesisNumber: '#ARC-2024-112',
    category: 'SÜPER ZEKA & HİZALAMA TEORİSİ',
    heroImage:
      'https://images.unsplash.com/photo-1620712943543-bcc4688e7485?q=80&w=1200&auto=format&fit=crop',
    author: {
      name: 'Ilya Sutskever',
      role: 'SSI Kurucusu & Eski OpenAI Baş Bilim İnsanı',
      avatarUrl:
        'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?q=80&w=200&auto=format&fit=crop',
    },
    executiveSummary: [
      'Ilya Sutskever, scaling laws (ölçekleme yasaları) sayesinde hesaplama ve veri arttıkça modellerin sadece ezberlemediğini, bilginin alt katmanlarındaki mantıksal nedenselliği çözdüğünü vurguluyor.',
      'İnsan seviyesini aşan yapay zeka (Süper Zeka) çağında insanların modelleri denetlemesi imkansızlaşacaktır. Bu nedenle insanüstü modelleri denetlemek için yapay zeka destekli hizalama araştırmacılarının geliştirilmesi şarttır.',
      'Güvenlik ve yetenek geliştirme birbirinden ayrı değil, eşzamanlı bir varoluşsal araştırma disiplini olarak yürütülmelidir.',
    ],
    findings: [
      {
        id: 'sf1',
        phase: '01 / ÖLÇEKLEME',
        timestamp: '[08:15]',
        timeSeconds: 495,
        title: 'Örüntüden Kavrama Geçişi',
        description:
          'Büyük ölçekte optimize edilen bir sonraki kelime tahmini, aslında fiziksel ve zihinsel durumların içsel bir simülasyonunu üretir.',
        confidence: 'Güven Derecesi: Yüksek (%99)',
      },
      {
        id: 'sf2',
        phase: '02 / SÜPER HİZALAMA',
        timestamp: '[22:04]',
        timeSeconds: 1324,
        title: 'Otomasyonel Denetim',
        description:
          'İnsan bilişini aşan akıl yürütmeleri denetleyebilecek özyinelemeli değerlendirme ağları kurulmalıdır.',
        confidence: 'Güven Derecesi: Stratejik İlke',
      },
    ],
    qaPairs: [
      {
        id: 'sqa1',
        question: 'Sutskever next-token prediction hakkında ne düşünüyor?',
        timeAgo: '3 saat önce',
        answer:
          'Bir sonraki kelimeyi tam olarak tahmin edebilmek için yazarın niyetini, psikolojisini, argüman yapısını ve dünyayı simüle etmek gerekir.',
        citation: 'Transkript [08:15 - 10:20]',
        citationTimestamp: '08:15',
      },
    ],
    transcript: [
      {
        time: '08:15',
        seconds: 495,
        speaker: 'Ilya Sutskever',
        text: 'Eğer bir sonraki kelimeyi mükemmel tahmin edebiliyorsanız, dedektif romanının sonunu da tahmin edebilmelisiniz. Bu da derin bir dünya anlayışı gerektirir.',
        highlighted: true,
      },
    ],
    chunks: [
      {
        id: 'schk-01',
        text: 'Ölçekleme yasaları: Compute-optimal Chinchilla parametreleri ve kayıp eğrisi tahminleri.',
        tokens: 512,
        dimension: 1536,
        similarity: 0.95,
        category: 'Ölçekleme Yasaları',
      },
    ],
    telemetry: {
      chunksCount: 54,
      referencedNamesCount: 16,
      wordsCount: '4.6k',
      vectorDimensions: 1536,
      matchPercentage: 100,
    },
    tags: ['AGI', 'Süper Zeka', 'Ölçekleme', 'OpenAI', 'Hizalama'],
    dateAdded: '2024-11-25',
  },
  {
    id: 'vaswani-attention-paper',
    type: 'paper',
    title: 'Vaswani et al. — Dikkat Mekanizması Yeterlidir (Attention Is All You Need)',
    originalUrl: 'https://arxiv.org/abs/1706.03762',
    duration: '18 Sayfa',
    reliability: 99.8,
    synthesisNumber: '#ARC-2024-001',
    category: 'TRANSFORMER MİMARİSİ & TEMEL MAKALE',
    heroImage:
      'https://images.unsplash.com/photo-1516116211227-bbc13c7a52e6?q=80&w=1200&auto=format&fit=crop',
    author: {
      name: 'Ashish Vaswani, Noam Shazeer et al.',
      role: 'Google Brain & Google Research',
      avatarUrl:
        'https://images.unsplash.com/photo-1534528741775-53994a69daeb?q=80&w=200&auto=format&fit=crop',
    },
    executiveSummary: [
      'Tekrarlayan (RNN/LSTM) ve evrişimli (CNN) sinir ağlarının yerini alan saf Multi-Head Self-Attention mimarisi, ardışık bağımlılıkları tamamen ortadan kaldırarak paralel eğitimi mümkün kılmıştır.',
      'Öz-dikkat (self-attention) mekanizması, bir cümledeki tüm kelimelerin mesafeden bağımsız olarak birbiriyle doğrudan ilişki kurmasına olanak tanır ve hesaplama karmaşıklığını O(1) yol uzunluğuna indirir.',
      'Bu makale ile atılan temeller, günümüzün tüm GPT, BERT, Claude ve Gemini modellerinin ortak mimari omurgasını oluşturmaktadır.',
    ],
    findings: [
      {
        id: 'vf1',
        phase: '01 / MULTI-HEAD',
        timestamp: '[Bölüm 3.2]',
        timeSeconds: 0,
        title: 'Çok Başlı Dikkat Katmanı',
        description:
          'Sorgu (Query), Anahtar (Key) ve Değer (Value) matrisleri farklı alt-uzaylarda eşzamanlı taranarak zengin anlamsal ilişkiler yakalanır.',
        confidence: 'Güven Derecesi: Matematiksel Kanıt',
      },
      {
        id: 'vf2',
        phase: '02 / POSITIONAL',
        timestamp: '[Bölüm 3.5]',
        timeSeconds: 0,
        title: 'Pozisyonel Kodlama',
        description:
          'Tekrarlama olmadığı için kelimelerin dizilim sırası sinüs ve kosinüs dalga boyu fonksiyonlarıyla vektörlere enjekte edilir.',
        confidence: 'Güven Derecesi: Yüksek (%99)',
      },
    ],
    qaPairs: [
      {
        id: 'vqa1',
        question: 'RNN’lere göre Transformer’ın en büyük avantajı nedir?',
        timeAgo: '1 gün önce',
        answer:
          'Ardışık işleme zorunluluğunun olmaması sayesinde tüm dizi aynı anda GPU tensor çekirdeklerinde paralel olarak eğitilebilir.',
        citation: 'Makale Bölüm 2 - Arka Plan',
        citationTimestamp: 'Bölüm 2',
      },
    ],
    transcript: [
      {
        time: 'Giriş',
        seconds: 0,
        speaker: 'Araştırma Özeti',
        text: 'Baskın dizi dönüştürme modelleri karmaşık tekrarlayan veya evrişimli sinir ağlarına dayanmaktadır. Biz tamamen dikkat mekanizmalarına dayanan yeni ve basit bir ağ mimarisi olan Transformer’ı öneriyoruz.',
        highlighted: true,
      },
    ],
    chunks: [
      {
        id: 'vchk-01',
        text: 'Scaled Dot-Product Attention: Attention(Q, K, V) = softmax(QK^T / sqrt(d_k)) V denkleminin türetimi ve bellek optimizasyonu.',
        tokens: 512,
        dimension: 1536,
        similarity: 0.98,
        category: 'Matematiksel Formülasyon',
      },
    ],
    telemetry: {
      chunksCount: 36,
      referencedNamesCount: 22,
      wordsCount: '6.1k',
      vectorDimensions: 1536,
      matchPercentage: 100,
    },
    tags: ['Transformer', 'Self-Attention', 'Deep Learning', 'Google Research'],
    dateAdded: '2024-11-01',
  },
];
