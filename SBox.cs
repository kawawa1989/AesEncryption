namespace Math;
/*
 有限体(ガロア体)計算をPythonで実装する
 https://qiita.com/HO_Pollyanna/items/ced3f9d40878d634bcd7
 
 拡張ユークリッドの互除法を2つの実装から理解する
 https://zenn.dev/senk/articles/8a230c83b5a614d175db
 
 
 https://en.wikipedia.org/wiki/Rijndael_S-box
 https://www.cqpub.co.jp/DWM/contest/2004/specification2004.pdf
 
注意: これらの計算はGF(2) の加法に基づく

素数2 で割った余りで分類される世界 ... GF(2)
-------------
+ | 0  1
0 | 0  1
1 | 1  0
-------------
であるため、加法はすべてXORである
そのため下記の + 記号はすべて XOR 演算とする。



S-boxは8ビットの入力cを8ビットの出力s = S(c)に写像する。
入力と出力はともにGF(2)上の多項式として解釈される。
まず、入力はRijndaelの有限体であるGF(2^8) = GF(2) [x]/(x^8 + x^4 + x^3 + x + 1)における乗法逆数に写像される。
恒等式である0はそれ自身に写像されます。
この変換は発明者Kaisa NybergにちなんでNyberg S-boxと呼ばれる[2]。




定数: 01100011(16進数で63)
① 各ビット列の番号を i とする。
② 定数 63(2進法で01100011) の各ビット値を c[i] とする。
③ 出力結果の値を s とする。
④ 入力値を b とする。

ここで、bは乗法逆数(※1)、
`＋` はビットごとのXOR演算子
`<<` は左ビットごとの循環シフト

※1. 逆数とは掛け合わせると1になる値のこと。
    例えば2の逆数は1/2, 1/b の逆数はbである。


【逆数の求め方】
ガロア体の要素【y】は
y^2^8-1 = y^255 = 1
という性質がある。
したがって

y^-1 * y^255 = y^254
となる y^254 を求めれば良い
NOTE: 累乗同士の掛け算は指数同士を足し算すれば良い

8ビットの値を考える
ある値A: a7x^7 + a6x^6 + a5x^5 + a4x^4 + a3x^3 + a2x^2 + a1x^1 + a0x^0
ある値B: b7x^7 + b6x^6 + b5x^5 + b4x^4 + b3x^3 + b2x^2 + b1x^1 + b0x^0
この二つをかける A・B = C

(a7x^7 + a6x^6 + a5x^5 + a4x^4 + a3x^3 + a2x^2 + a1x^1 + a0x^0) × 
(b7x^7 + b6x^6 + b5x^5 + b4x^4 + b3x^3 + b2x^2 + b1x^1 + b0x^0)

[a7b7]x^14 + [a7b6]x^13 + [a7b5]x^12 + [a7b4]x^11 + [a7b3]x^10 + [a7b2]x^9 + [a7b1]x^8 + [a7b0]x^7 +
[a6b7]x^13 + [a6b6]x^12 + [a6b5]x^11 + [a6b4]x^10 + [a6b3]x^9  + [a6b2]x^8 + [a6b1]x^7 + [a6b0]x^6 +
[a5b7]x^12 + [a5b6]x^11 + [a5b5]x^10 + [a5b4]x^9  + [a5b3]x^8  + [a5b2]x^7 + [a5b1]x^6 + [a5b0]x^5 +
[a4b7]x^11 + [a4b6]x^10 + [a4b5]x^9  + [a4b4]x^8  + [a4b3]x^7  + [a4b2]x^6 + [a4b1]x^5 + [a4b0]x^4 +
[a3b7]x^10 + [a3b6]x^9  + [a3b5]x^8  + [a3b4]x^7  + [a3b3]x^6  + [a3b2]x^5 + [a3b1]x^4 + [a3b0]x^3 +
[a2b7]x^9  + [a2b6]x^8  + [a2b5]x^7  + [a2b4]x^6  + [a2b3]x^5  + [a2b2]x^4 + [a2b1]x^3 + [a2b0]x^2 +
[a1b7]x^8  + [a1b6]x^7  + [a1b5]x^6  + [a1b4]x^5  + [a1b3]x^4  + [a1b2]x^3 + [a1b1]x^2 + [a1b0]x^1 +
[a0b7]x^7  + [a0b6]x^6  + [a0b5]x^5  + [a0b4]x^4  + [a0b3]x^3  + [a0b2]x^2 + [a0b1]x^1 + [a0b0]x^0 +

以下のように累乗の指数同士でまとめる
[c14]x^14 ... [a7b7]
[c13]x^13 ... [a7b6] + [a6b7]
[c12]x^12 ... [a7b5] + [a6b6] + [a5b7]
[c11]x^11 ... [a7b4] + [a6b5] + [a5b6] + [a4b7]
[c10]x^10 ... [a7b3] + [a6b4] + [a5b5] + [a4b6] + [a3b7]
[c9]x^9   ... [a7b2] + [a6b3] + [a5b4] + [a4b5] + [a3b6] + [a2b7]
[c8]x^8   ... [a7b1] + [a6b2] + [a5b3] + [a4b4] + [a3b5] + [a2b6] + [a1b7]
[c7]x^7   ... [a7b0] + [a6b1] + [a5b2] + [a4b3] + [a3b4] + [a2b5] + [a1b6] + [a0b7]
[c6]x^6   ... [a6b0] + [a5b1] + [a4b2] + [a3b3] + [a2b4] + [a1b5] + [a0b6]
[c5]x^5   ... [a5b0] + [a4b1] + [a3b2] + [a2b3] + [a1b4] + [a0b5]
[c4]x^4   ... [a4b0] + [a3b1] + [a2b2] + [a1b3] + [a0b4]
[c3]x^3   ... [a3b0] + [a2b1] + [a1b2] + [a0b3]
[c2]x^2   ... [a2b0] + [a1b1] + [a0b2]
[c1]x^1   ... [a1b0] + [a0b1]
[c0]x^0   ... [a0b0]

よって
C(x) = 
    [c14]x^14 + [c13]x^13 + [c12]x^12 + [c11]x^11 + [c10]x^10 + [c9]x^9 + [c8]x^8 + 
    [c7]x^7 + [c6]x^6 + [c5]x^5 + [c4]x^4 + [c3]x^3 + [c2]x^2 + [c1]x^1 + [c0]x^0

既約多項式 【x^8 + x^4 + x^3 + x + 1】 のある 根a が存在するとき
a^8 + a^4 + a^3 + a + 1 = 0
これを
a^8 + a^4 - a^4 + a^3 - a^3 + a - a + 1 - 1 = -a^4 - a^3 - a - 1
GF(2) では 1 + 1 = 0であることから
1 + 1 = 0
1 + 1 - 1 = -1
1 = -1
であるため、

a^8 + a^4 + a^4 + a^3 + a^3 + a + a + 1 + 1 = a^4 + a^3 + a + 1
と現せる。

従って
a^8 = a^4 + a^3 + a + 1
である。

以下のように乗算での関係式を作る
0    ... 0
x^0  ... 1
x^1  ... x
x^2  ... x^2
x^3  ... x^3
x^4  ... x^4
x^5  ... x^5
x^6  ... x^6
x^7  ... x^7
x^8  ... x^4 + x^3 + x + 1
x^9  ... x^5 + x^4 + x^2 + x
x^10 ... x^6 + x^5 + x^3 + x^2
x^11 ... x^7 + x^6 + x^4 + x^3
x^12 ... x^7 + x^5 + x^3 + x + 1
x^13 ... x^6 + x^3 + x^2 + 1
x^14 ... x^7 + x^4 + x^3 + x

これを先ほどの式に当てはめる
D(x) = [d7]x^7 + [d6]x^6 + [d5]x^5 + [d4]x^4 + [d3]x^3 + [d2]x^2 + [d1]x^1 + [d0]x^0
という多項式を求めることができる。

d7 ... [c14]x^7 + [c12]x^7 + [c11]x^7 + [c7 ]x^7
d6 ... [c13]x^6 + [c11]x^6 + [c10]x^6 + [c6 ]x^6
d5 ... [c12]x^5 + [c10]x^5 + [c9 ]x^5 + [c5 ]x^5
d4 ... [c14]x^4 + [c11]x^4 + [c9 ]x^4 + [c8 ]x^4 + [c4]x^4
d3 ... [c14]x^3 + [c13]x^3 + [c12]x^3 + [c11]x^3 + [c10]x^3 + [c8]x^3 + [c3]x^3
d2 ... [c13]x^2 + [c10]x^2 + [c9 ]x^2 + [c2 ]x^2
d1 ... [c14]x^1 + [c12]x^1 + [c9 ]x^1 + [c8 ]x^1 + [c1]x^1
d0 ... [c13]x^0 + [c12]x^0 + [c8 ]x^0 + [c0 ]x^0


定数63（16）＝01100011（2）は16進数で示される。
アフィン変換の等価式は次の通りである。
このアフィン変換は、バイトをベクトルとして複数回回転させたものの和であり、加算はXOR演算となる。

アフィン変換式
s[i] = b[i] + b[(i + 4) % 8] + b[(i + 5) % 8] + b[(i + 6) % 8] + b[(i + 7) % 8] + c[i]

従って、以下のようになる。
s[0] = b[0] + b[4] + b[5] + b[6] + b[7] + c[0]
s[1] = b[0] + b[1] + b[5] + b[6] + b[7] + c[1]
s[2] = b[0] + b[1] + b[2] + b[6] + b[7] + c[2]
s[3] = b[0] + b[1] + b[2] + b[3] + b[7] + c[3]
s[4] = b[0] + b[1] + b[2] + b[3] + b[4] + c[4]
s[5] = b[1] + b[2] + b[3] + b[4] + b[5] + c[5]
s[6] = b[2] + b[3] + b[4] + b[5] + b[6] + c[6]
s[7] = b[3] + b[4] + b[5] + b[6] + b[7] + c[7]
 */

public class SBox
{
    
}