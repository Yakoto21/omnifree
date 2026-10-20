#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
generate_dynamic_fixtures.py
Gera fixtures de teste para a Fase 12.6 do Lodino (DEFLATE Huffman Dinâmico, Fixo e Stored).
Classifica os arquivos em 4 matrizes:
1. Dynamic Huffman (BTYPE 10): Texto natural e estruturado com assimetria de frequências.
2. Fixed Huffman (BTYPE 01): Payloads pequenos onde a sobrecarga do cabeçalho dinâmico não compensa.
3. Stored (BTYPE 00): Dados de alta entropia / incompressíveis onde Huffman gera expansão.
4. Benchmark Misto: Dados compostos para medição de ganho e taxa de compressão.
"""

import os
import sys
import math
import hashlib
import json
import random

def calculate_entropy(data: bytes) -> float:
    if not data:
        return 0.0
    freq = {}
    for b in data:
        freq[b] = freq.get(b, 0) + 1
    total = len(data)
    ent = 0.0
    for count in freq.values():
        p = count / total
        ent -= p * math.log2(p)
    return ent

def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    fixtures_dir = os.path.join(script_dir, "fixtures")
    os.makedirs(fixtures_dir, exist_ok=True)

    categories = {
        "dynamic": os.path.join(fixtures_dir, "dynamic_btype10"),
        "fixed": os.path.join(fixtures_dir, "fixed_btype01"),
        "stored": os.path.join(fixtures_dir, "stored_btype00"),
        "mixed": os.path.join(fixtures_dir, "mixed_bench")
    }

    for cat_path in categories.values():
        os.makedirs(cat_path, exist_ok=True)

    # Deterministic random generator for reproducibility
    rng = random.Random(0x1951_1206)

    manifest = []

    # =========================================================================
    # 1. DYNAMIC HUFFMAN (BTYPE 10) FIXTURES
    # =========================================================================
    # 1.1 dyn_english_prose.txt (~6 KB): Natural English text with skewed frequencies
    prose_sample = """The DEFLATE Compressed Data Format Specification (RFC 1951) defines a lossless compressed
data format that compresses data using a combination of the LZ77 algorithm and Huffman coding.
The data can be produced or consumed, even for an arbitrarily long sequentially presented input data stream,
using only an a priori bounded amount of intermediate storage. The format can be implemented readily in a
manner not covered by patents. DEFLATE consists of a series of blocks, corresponding to successive blocks
of input data. Each block is compressed using either fixed Huffman codes, dynamic Huffman codes, or no compression.
Dynamic Huffman codes use custom prefix codes computed specifically for the symbol frequencies observed in the
uncompressed block. For natural language prose, where letters like 'e', 't', 'a', 'o', 'i', 'n', 's', 'h', and 'r'
appear with significantly higher probability than rare characters like 'q', 'x', and 'z', dynamic Huffman coding
achieves remarkable compaction compared to the standard predefined code lengths of fixed Huffman coding.
In fixed Huffman coding, literals 0 through 143 require 8 bits, and literals 144 through 255 require 9 bits.
However, in dynamic Huffman coding, an optimal prefix code for the letter 'e' or space can be as short as 3 or 4 bits!
This creates a substantial compression advantage whenever the payload is large enough to amortize the tree header."""
    # Repeat prose to ~6 KB
    dyn_prose = (prose_sample + "\n\n") * 6
    p1 = os.path.join(categories["dynamic"], "dyn_english_prose.txt")
    with open(p1, "wb") as f:
        f.write(dyn_prose.encode("utf-8"))

    # 1.2 dyn_portuguese_corpus.txt (~5 KB): Natural Portuguese text with diacritics
    pt_sample = """A compressão de dados é uma tecnologia fundamental para a eficiência da computação moderna.
No contexto do projeto Lodino, a implementação proprietária do algoritmo DEFLATE conforme a RFC 1951
permite que documentos em língua portuguesa sejam comprimidos com alto grau de compactação sem perdas.
O idioma português possui características estatísticas muito marcantes: vogais como 'a', 'e' e 'o' possuem
altíssima frequência relativa, seguidas por consoantes como 's', 'r', 'd', 'm' e 't'. Além disso, o uso
frequente de artigos definidos e indefinidos ('o', 'a', 'os', 'as', 'um', 'uma'), preposições ('de', 'em', 'para')
e conjunções cria repetições recorrentes que são exploradas com grande eficiência tanto pelo LZ77 quanto pelas
tabelas de Huffman dinâmicas. Caracteres acentuados como 'á', 'é', 'í', 'ó', 'ú', 'ã', 'õ' e 'ç' são representados
em UTF-8 com 2 bytes cada, gerando pares de bytes característicos que o algoritmo agrupa e codifica com árvores
adaptadas sob medida para o texto analisado."""
    dyn_pt = (pt_sample + "\n\n") * 5
    p2 = os.path.join(categories["dynamic"], "dyn_portuguese_corpus.txt")
    with open(p2, "wb") as f:
        f.write(dyn_pt.encode("utf-8"))

    # 1.3 dyn_rest_payload.json (~10 KB): REST API JSON with recurring schema keys
    json_items = []
    statuses = ["active", "pending", "suspended", "verified"]
    roles = ["administrator", "operator", "auditor", "viewer"]
    regions = ["sa-east-1", "us-east-1", "eu-central-1", "ap-southeast-1"]
    for i in range(1, 45):
        json_items.append({
            "id": i,
            "uuid": f"usr-2026-{i:05d}-lodino",
            "username": f"user_account_{i}",
            "email": f"operator_{i}@datacenter.lodino.internal",
            "role": roles[i % len(roles)],
            "status": statuses[i % len(statuses)],
            "region": regions[i % len(regions)],
            "quota_limit_mb": 1024 * (i % 8 + 1),
            "quota_used_mb": 128 * (i % 7),
            "is_mfa_enabled": (i % 2 == 0),
            "created_at": "2026-10-19T14:30:00Z",
            "last_login_at": "2026-10-19T22:15:33Z",
            "tags": ["production", "lodino-phase-12", "deflate-testing"]
        })
    dyn_json = json.dumps({"status": "success", "total_records": len(json_items), "data": json_items}, indent=2)
    p3 = os.path.join(categories["dynamic"], "dyn_rest_payload.json")
    with open(p3, "wb") as f:
        f.write(dyn_json.encode("utf-8"))

    # 1.4 dyn_source_code.c (~8 KB): Clean C source code with keywords and syntax
    dyn_c = """/* =========================================================================
 * Lodino Core - Huffman Dynamic Deflate Reference Implementation
 * ========================================================================= */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>

#define DEFLATE_MAX_LITERALS 286
#define DEFLATE_MAX_DISTANCES 30
#define DEFLATE_MAX_CODE_LENGTHS 19
#define WINDOW_SIZE 32768
#define HASH_SIZE 65536

typedef struct {
    uint16_t code;
    uint8_t length;
} HuffmanCode;

typedef struct {
    HuffmanCode lit_table[DEFLATE_MAX_LITERALS];
    HuffmanCode dist_table[DEFLATE_MAX_DISTANCES];
    uint32_t lit_freq[DEFLATE_MAX_LITERALS];
    uint32_t dist_freq[DEFLATE_MAX_DISTANCES];
    int hlit;
    int hdist;
    int hclen;
} DynamicBlockHeader;

int calculate_optimal_block_type(const uint8_t* buffer, size_t size, int* out_btype) {
    if (buffer == NULL || size == 0) {
        *out_btype = 0; // Stored
        return 0;
    }
    size_t stored_cost = size * 8 + 40;
    size_t fixed_cost = 3;
    size_t dynamic_cost = 3 + 14; // Base header bits

    /* Count frequencies and compute Shannon entropy */
    uint32_t counts[256] = {0};
    for (size_t i = 0; i < size; i++) {
        counts[buffer[i]]++;
    }

    double entropy = 0.0;
    for (int i = 0; i < 256; i++) {
        if (counts[i] > 0) {
            double p = (double)counts[i] / (double)size;
            entropy -= p * (log(p) / log(2.0));
        }
    }

    /* Decision logic: */
    if (entropy > 7.92 && size > 512) {
        *out_btype = 0; // Incompressible data -> Stored (BTYPE 00)
        return 0;
    }
    if (size < 120) {
        *out_btype = 1; // Small payload -> Fixed Huffman (BTYPE 01)
        return 0;
    }
    *out_btype = 2; // Natural text / code -> Dynamic Huffman (BTYPE 10)
    return 0;
}
""" * 4
    p4 = os.path.join(categories["dynamic"], "dyn_source_code.c")
    with open(p4, "wb") as f:
        f.write(dyn_c.encode("utf-8"))

    # 1.5 dyn_svg_vector.svg (~7 KB): XML SVG vector file
    svg_paths = []
    for i in range(25):
        cx = 50 + (i * 20) % 500
        cy = 50 + (i * 15) % 400
        r = 10 + (i % 15)
        color = f"#{((i*37)%256):02x}{((i*67)%256):02x}{((i*97)%256):02x}"
        svg_paths.append(f'  <circle cx="{cx}" cy="{cy}" r="{r}" fill="{color}" stroke="#333333" stroke-width="1.5" />')
        svg_paths.append(f'  <path d="M {cx} {cy} L {cx+30} {cy+30} L {cx-30} {cy+30} Z" fill="{color}" fill-opacity="0.6" />')
    dyn_svg = f"""<?xml version="1.0" encoding="UTF-8" standalone="no"?>
<svg xmlns="http://www.w3.org/2000/svg" width="800" height="600" viewBox="0 0 800 600">
  <title>Lodino Dynamic Huffman Vector Graphic Fixture</title>
  <rect width="100%" height="100%" fill="#f4f6f8" />
  <g id="vector-layer-1" transform="translate(20,20)">
{os.linesep.join(svg_paths)}
  </g>
</svg>"""
    p5 = os.path.join(categories["dynamic"], "dyn_svg_vector.svg")
    with open(p5, "wb") as f:
        f.write(dyn_svg.encode("utf-8"))

    # =========================================================================
    # 2. FIXED HUFFMAN (BTYPE 01) FIXTURES
    # =========================================================================
    # 2.1 fixed_tiny_phrase.txt (44 bytes): Classic short phrase
    f1 = "The quick brown fox jumps over the lazy dog."
    p21 = os.path.join(categories["fixed"], "fixed_tiny_phrase.txt")
    with open(p21, "wb") as f:
        f.write(f1.encode("utf-8"))

    # 2.2 fixed_small_json.json (71 bytes): Minimal JSON config
    f2 = '{"status": "ok", "code": 200, "message": "done", "active": true, "id": 101}'
    p22 = os.path.join(categories["fixed"], "fixed_small_json.json")
    with open(p22, "wb") as f:
        f.write(f2.encode("utf-8"))

    # 2.3 fixed_short_log.log (51 bytes): Single short server log line
    f3 = "[2026-10-19 23:45:10] INFO Worker: Port 8080 ready."
    p23 = os.path.join(categories["fixed"], "fixed_short_log.log")
    with open(p23, "wb") as f:
        f.write(f3.encode("utf-8"))

    # 2.4 fixed_uniform_ascii.txt (95 bytes): Uniform printable ASCII chars (32..126)
    f4 = bytes(range(32, 127))
    p24 = os.path.join(categories["fixed"], "fixed_uniform_ascii.txt")
    with open(p24, "wb") as f:
        f.write(f4)

    # 2.5 fixed_small_greeting.txt (38 bytes): Tiny greeting
    f5 = "Lodino ZIP Engine: Fixed Huffman Test."
    p25 = os.path.join(categories["fixed"], "fixed_small_greeting.txt")
    with open(p25, "wb") as f:
        f.write(f5.encode("utf-8"))

    # =========================================================================
    # 3. STORED (BTYPE 00) FIXTURES
    # =========================================================================
    # 3.1 stored_random_high_entropy.bin (4096 bytes): High entropy CSPRNG bytes
    s1 = bytearray(4096)
    for i in range(4096):
        s1[i] = rng.getrandbits(8)
    p31 = os.path.join(categories["stored"], "stored_random_high_entropy.bin")
    with open(p31, "wb") as f:
        f.write(s1)

    # 3.2 stored_compressed_jpeg_slice.bin (8192 bytes): High entropy pre-compressed entropy data
    # Generated with variable-length random bit-twiddling simulating Huffman DCT codes
    s2 = bytearray(8192)
    state = 0xDEADBEEF
    for i in range(8192):
        state = (state * 1664525 + 1013904223) & 0xFFFFFFFF
        s2[i] = (state >> 16) & 0xFF
    p32 = os.path.join(categories["stored"], "stored_compressed_jpeg_slice.bin")
    with open(p32, "wb") as f:
        f.write(s2)

    # 3.3 stored_aes_ciphertext.bin (2048 bytes): Simulated AES-256 CBC ciphertext
    s3 = bytearray(2048)
    for i in range(2048):
        s3[i] = rng.randint(0, 255)
    p33 = os.path.join(categories["stored"], "stored_aes_ciphertext.bin")
    with open(p33, "wb") as f:
        f.write(s3)

    # 3.4 stored_sha256_table.dat (4096 bytes): 128 concatenated binary SHA-256 digests
    s4 = bytearray()
    for i in range(128):
        h = hashlib.sha256(f"digest_block_seed_{i}".encode("utf-8")).digest()
        s4.extend(h)
    p34 = os.path.join(categories["stored"], "stored_sha256_table.dat")
    with open(p34, "wb") as f:
        f.write(s4)

    # =========================================================================
    # 4. MIXED / BENCHMARK FIXTURES
    # =========================================================================
    # 4.1 mixed_csv_table.csv (~16 KB): CSV table with repetitive columns and numeric data
    csv_rows = ["id,customer_name,country_code,signup_date,plan_tier,monthly_fee,status,auth_hash"]
    countries = ["BR", "US", "DE", "JP", "FR", "GB", "CA"]
    plans = ["free", "pro", "enterprise", "team"]
    for i in range(1, 201):
        c = countries[i % len(countries)]
        p = plans[i % len(plans)]
        fee = 0.00 if p == "free" else (29.99 if p == "pro" else (99.00 if p == "team" else 299.00))
        h = hashlib.md5(f"user_{i}".encode()).hexdigest()[:12]
        csv_rows.append(f"{i},Customer_{i:04d},{c},2026-01-{(i%28)+1:02d},{p},{fee:.2f},active,{h}")
    m1 = os.linesep.join(csv_rows)
    p41 = os.path.join(categories["mixed"], "mixed_csv_table.csv")
    with open(p41, "wb") as f:
        f.write(m1.encode("utf-8"))

    # 4.2 mixed_repetitive_runs.txt (32 KB): Highly repetitive runs with periodic variation
    m2_chunks = []
    for i in range(200):
        m2_chunks.append(f"HEADER_BLOCK_{i:03d}: [AAAAABBBBBCCCCCDDDDDEEEEEFFFFFGGGGGHHHHHIIIIIJJJJJKKKKK]\n")
        m2_chunks.append("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt.\n")
    m2 = "".join(m2_chunks)
    p42 = os.path.join(categories["mixed"], "mixed_repetitive_runs.txt")
    with open(p42, "wb") as f:
        f.write(m2.encode("utf-8"))

    # 4.3 mixed_html_page.html (~12 KB): Web page with HTML, CSS, JavaScript, text
    p43 = os.path.join(categories["mixed"], "mixed_html_page.html")
    m3 = f"""<!DOCTYPE html>
<html lang="pt-BR">
<head>
  <meta charset="UTF-8">
  <title>Lodino DEFLATE Compression Test Page</title>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; margin: 40px; background: #fafafa; color: #333; }}
    .container {{ max-width: 960px; margin: auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
    h1 {{ color: #0066cc; border-bottom: 2px solid #eaeaea; padding-bottom: 10px; }}
    table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
    th, td {{ padding: 12px; border: 1px solid #ddd; text-align: left; }}
    th {{ background: #f2f7fc; }}
    .badge {{ display: inline-block; padding: 4px 8px; border-radius: 4px; font-weight: bold; }}
    .badge-dynamic {{ background: #d4edda; color: #155724; }}
    .badge-fixed {{ background: #cce5ff; color: #004085; }}
    .badge-stored {{ background: #fff3cd; color: #856404; }}
  </style>
</head>
<body>
  <div class="container">
    <h1>Relatório de Desempenho DEFLATE Fase 12.6</h1>
    <p>Esta página simula a composição padrão de um artefato Web moderno com marcação estruturada, folhas de estilo CSS, scripts interativos e tabelas de dados tabulares.</p>
    <table>
      <thead>
        <tr><th>ID</th><th>Módulo</th><th>Algoritmo</th><th>Estado</th></tr>
      </thead>
      <tbody>
"""
    for i in range(1, 35):
        m3 += f"""        <tr><td>{i}</td><td>Compressor.Deflate.{i}</td><td>Huffman Dinâmico RFC 1951</td><td><span class="badge badge-dynamic">ATIVO</span></td></tr>\n"""
    m3 += """      </tbody>
    </table>
  </div>
  <script>
    console.log("Lodino Phase 12.6 Test Runner Loaded successfully.");
  </script>
</body>
</html>"""
    with open(p43, "wb") as f:
        f.write(m3.encode("utf-8"))

    # =========================================================================
    # BUILD MANIFEST WITH SHA-256 AND ENTROPY METRICS
    # =========================================================================
    for cat_name, cat_dir in categories.items():
        for filename in sorted(os.listdir(cat_dir)):
            filepath = os.path.join(cat_dir, filename)
            if os.path.isfile(filepath):
                with open(filepath, "rb") as f:
                    content = f.read()
                sha256 = hashlib.sha256(content).hexdigest()
                entropy = calculate_entropy(content)
                rel_path = os.path.relpath(filepath, script_dir).replace("\\", "/")
                
                # Expected block type rationale
                if cat_name == "dynamic":
                    expected_btype = "10 (Dynamic Huffman)"
                    rationale = f"Tamanho ({len(content)} bytes) amortiza cabeçalho dinâmico; assimetria de frequências (entropia: {entropy:.2f} bits/byte)."
                elif cat_name == "fixed":
                    expected_btype = "01 (Fixed Huffman)"
                    rationale = f"Tamanho pequeno ({len(content)} bytes); sobrecarga do cabeçalho dinâmico (~40-60B) excede ganho."
                elif cat_name == "stored":
                    expected_btype = "00 (Stored / Uncompressed)"
                    rationale = f"Alta entropia ({entropy:.2f} bits/byte); codificação Huffman causa expansão de tamanho."
                else:
                    expected_btype = "10 (Dynamic Huffman)"
                    rationale = f"Dados compostos/estruturados ({len(content)} bytes, entropia {entropy:.2f}); LZ77 + Dynamic otimizam saída."

                manifest.append({
                    "category": cat_name,
                    "filename": filename,
                    "rel_path": rel_path,
                    "size_bytes": len(content),
                    "entropy_bits": round(entropy, 4),
                    "sha256": sha256,
                    "expected_btype": expected_btype,
                    "rationale": rationale
                })

    # Save JSON and TXT manifests
    manifest_json_path = os.path.join(script_dir, "fixtures_manifest.json")
    with open(manifest_json_path, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)

    manifest_txt_path = os.path.join(script_dir, "fixtures_manifest.txt")
    with open(manifest_txt_path, "w", encoding="utf-8") as f:
        f.write(f"{'Categoria':<10} | {'Arquivo':<35} | {'Bytes':>7} | {'Entropia':>8} | {'BTYPE Esperado':<22} | {'SHA-256':<64}\n")
        f.write("-" * 155 + "\n")
        for item in manifest:
            f.write(f"{item['category']:<10} | {item['filename']:<35} | {item['size_bytes']:>7} | {item['entropy_bits']:>8.4f} | {item['expected_btype']:<22} | {item['sha256']:<64}\n")

    print(f"[OK] Geradas {len(manifest)} fixtures com sucesso em {fixtures_dir}")
    print(f"[OK] Manifesto JSON: {manifest_json_path}")
    print(f"[OK] Manifesto TXT:  {manifest_txt_path}")

if __name__ == "__main__":
    main()
