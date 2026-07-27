module mmc1 (  SRAM_CE, PRG_A16, PRG_nCE, PRG_A17, CHR_A13, CHR_A15, CHR_A14, CHR_A12, PPU_A11, PPU_A10, M2, nROMSEL, PPU_A12, CPU_D7, CPU_D0, PRG_A14, PRG_A15, CHR_A16, CPU_A14, CPU_A13, CPU_RnW, CIRAM_A10);

	output wire SRAM_CE;
	output wire PRG_A16;
	output wire PRG_nCE;
	output wire PRG_A17;
	output wire CHR_A13;
	output wire CHR_A15;
	output wire CHR_A14;
	output wire CHR_A12;
	input wire PPU_A11;
	input wire PPU_A10;
	input wire M2;
	input wire nROMSEL;
	input wire PPU_A12;
	input wire CPU_D7;
	input wire CPU_D0;
	output wire PRG_A14;
	output wire PRG_A15;
	output wire CHR_A16;
	input wire CPU_A14;
	input wire CPU_A13;
	input wire CPU_RnW;
	output wire CIRAM_A10;

	// Wires

	wire w1;
	wire w2;
	wire w3;
	wire w4;
	wire w5;
	wire w6;
	wire w7;
	wire w8;
	wire w9;
	wire w10;
	wire w11;
	wire w12;
	wire w13;
	wire w14;
	wire w15;
	wire w16;
	wire w17;
	wire w18;
	wire w19;
	wire w20;
	wire w21;
	wire w22;
	wire w23;
	wire w24;
	wire w25;
	wire w26;
	wire w27;
	wire w28;
	wire w29;
	wire w30;
	wire w31;
	wire w32;
	wire w33;
	wire w34;
	wire w35;
	wire w36;
	wire w37;
	wire w38;
	wire w39;
	wire w40;
	wire w41;
	wire w42;
	wire w43;
	wire w44;
	wire w45;
	wire w46;
	wire w47;
	wire w48;
	wire w49;
	wire w50;
	wire w51;
	wire w52;
	wire w53;
	wire w54;
	wire w55;
	wire w56;
	wire w57;
	wire w58;
	wire w59;
	wire w60;
	wire w61;
	wire w62;
	wire w63;
	wire w64;
	wire w65;
	wire w66;
	wire w67;
	wire w68;
	wire w69;
	wire w70;
	wire w71;
	wire w72;
	wire w73;
	wire w74;
	wire w75;
	wire w76;
	wire w77;
	wire w78;
	wire w79;
	wire w80;
	wire w81;
	wire w82;
	wire w83;
	wire w84;
	wire w85;
	wire w86;
	wire w87;
	wire w88;
	wire w89;
	wire w90;
	wire w91;
	wire w92;
	wire w93;
	wire w94;
	wire w95;
	wire w96;
	wire w97;
	wire w98;
	wire w99;
	wire w100;
	wire w101;
	wire w102;
	wire w103;
	wire w104;
	wire w105;
	wire w106;
	wire w107;
	wire w108;
	wire w109;
	wire w110;
	wire w111;
	wire w112;
	wire w113;
	wire w114;
	wire w115;
	wire w116;
	wire w117;
	wire w118;
	wire w119;
	wire w120;
	wire w121;
	wire w122;
	wire w123;
	wire w124;
	wire w125;
	wire w126;
	wire w127;
	wire w128;
	wire w129;
	wire w130;
	wire w131;
	wire w132;
	wire w133;
	wire w134;
	wire w135;
	wire w136;
	wire w137;
	wire w138;
	wire w139;
	wire w140;
	wire w141;
	wire w142;
	wire w143;
	wire w144;
	wire w145;
	wire w146;
	wire w147;
	wire w148;
	wire w149;
	wire w150;
	wire w151;
	wire w152;
	wire w153;
	wire w154;
	wire w155;
	wire w156;
	wire w157;
	wire w158;
	wire w159;

	assign SRAM_CE = w15;
	assign PRG_A16 = w16;
	assign PRG_nCE = w6;
	assign PRG_A17 = w2;
	assign CHR_A13 = w19;
	assign CHR_A15 = w17;
	assign CHR_A14 = w18;
	assign CHR_A12 = w27;
	assign w62 = PPU_A11;
	assign w97 = PPU_A10;
	assign w125 = M2;
	assign w134 = nROMSEL;
	assign w47 = PPU_A12;
	assign w130 = CPU_D7;
	assign w120 = CPU_D0;
	assign PRG_A14 = w84;
	assign PRG_A15 = w111;
	assign CHR_A16 = w143;
	assign w113 = CPU_A14;
	assign w105 = CPU_A13;
	assign w150 = CPU_RnW;
	assign CIRAM_A10 = w92;

	// Instances

	mmc1_not g1 (.a(w125), .x(w156) );
	mmc1_and g2 (.a(w118), .b(w125), .x(w15) );
	mmc1_or g3 (.a(w133), .b(w134), .x(w6) );
	mmc1_not2 g4 (.a(w156), .x(w38) );
	mmc1_buf2 g5 (.a(w156), .x(w39) );
	mmc1_buf g6 (.a(w135), .x(w152) );
	mmc1_buf g7 (.a(w152), .x(w153) );
	mmc1_buf g8 (.a(w153), .x(w154) );
	mmc1_buf g9 (.a(w154), .x(w155) );
	mmc1_buf g10 (.a(w136), .x(w135) );
	mmc1_and g11 (.a(w155), .b(w131), .x(w119) );
	mmc1_buf g12 (.a(w126), .x(w136) );
	mmc1_dff g13 (.d(w129), .cck(w38), .ck(w39), .q(w86) );
	mmc1_nand g14 (.a(w40), .b(w127), .x(w151) );
	mmc1_nand g15 (.a(w127), .b(w41), .x(w43) );
	mmc1_buf g16 (.a(w134), .x(w128) );
	mmc1_not g17 (.a(w149), .x(w127) );
	mmc1_buf g18 (.a(w42), .x(w149) );
	mmc1_nor g19 (.a(w43), .b(w37), .x(w45) );
	mmc1_3or g20 (.a(w44), .b(w158), .c(w150), .x(w129) );
	mmc1_not g21 (.a(w150), .x(w133) );
	mmc1_not g22 (.a(w130), .x(w44) );
	mmc1_buf g23 (.a(w132), .x(w126) );
	mmc1_buf g24 (.a(w128), .x(w121) );
	mmc1_nor g25 (.a(w150), .b(w158), .x(w48) );
	mmc1_dff g26 (.d(w48), .cck(w38), .ck(w39), .q(w50) );
	mmc1_buf g27 (.a(w121), .x(w122) );
	mmc1_buf g28 (.a(w122), .x(w123) );
	mmc1_buf g29 (.a(w123), .x(w158) );
	mmc1_buf g30 (.a(w124), .x(w71) );
	mmc1_buf g31 (.a(w56), .x(w132) );
	mmc1_buf g32 (.a(w71), .x(w55) );
	mmc1_buf g33 (.a(w55), .x(w131) );
	mmc1_buf g34 (.a(w57), .x(w56) );
	mmc1_buf g35 (.a(w61), .x(w57) );
	mmc1_buf g36 (.a(w131), .x(w59) );
	mmc1_buf g37 (.a(w60), .x(w61) );
	mmc1_buf g38 (.a(w59), .x(w58) );
	mmc1_buf g39 (.a(w70), .x(w60) );
	mmc1_buf g40 (.a(w58), .x(w69) );
	mmc1_buf g41 (.a(w68), .x(w70) );
	mmc1_buf g42 (.a(w69), .x(w67) );
	mmc1_buf g43 (.a(w66), .x(w68) );
	mmc1_buf g44 (.a(w67), .x(w64) );
	mmc1_buf g45 (.a(w65), .x(w66) );
	mmc1_buf g46 (.a(w63), .x(w65) );
	mmc1_buf g47 (.a(w64), .x(w63) );
	mmc1_aon g48 (.a0(w110), .a1(w113), .b(w114), .x(w16) );
	mmc1_4and g49 (.a(w134), .b(w113), .c(w105), .d(w119), .x(w118) );
	mmc1_aon g50 (.a0(w112), .a1(w113), .b(w157), .x(w111) );
	mmc1_buf g51 (.a(w38), .x(w87) );
	mmc1_dffr g52 (.nres(w86), .d(w42), .cck(w39), .ck(w38), .q(w36) );
	mmc1_dffnq g53 (.d(w113), .cck(w76), .ck(w75), .nq(w37), .q(w116) );
	mmc1_dffnq g54 (.d(w105), .cck(w76), .ck(w75), .nq(w40), .q(w41) );
	mmc1_nor g55 (.a(w116), .b(w151), .x(w108) );
	mmc1_nor g56 (.a(w151), .b(w37), .x(w115) );
	mmc1_nor g57 (.a(w43), .b(w116), .x(w24) );
	mmc1_buf g58 (.a(w45), .x(w104) );
	mmc1_buf g59 (.a(w137), .x(w124) );
	mmc1_buf g60 (.a(w46), .x(w42) );
	mmc1_not g61 (.a(w45), .x(w117) );
	mmc1_dff g62 (.d(w120), .cck(w38), .ck(w39), .q(w49) );
	mmc1_dffrnq g63 (.nres(w36), .d(w51), .cck(w76), .ck(w75), .q(w52), .nq(w51) );
	mmc1_dffrnq g64 (.nres(w36), .d(w53), .cck(w52), .ck(w51), .q(w72), .nq(w53) );
	mmc1_dffrnq g65 (.nres(w36), .d(w54), .cck(w72), .ck(w53), .q(w102), .nq(w54) );
	mmc1_3nand g66 (.a(w52), .b(w53), .c(w102), .x(w46) );
	mmc1_not g67 (.a(w100), .x(w101) );
	mmc1_buf g68 (.a(w108), .x(w81) );
	mmc1_not g69 (.a(w108), .x(w83) );
	mmc1_aon g70 (.a0(w109), .a1(w79), .b(w82), .x(w114) );
	mmc1_oan g71 (.a0(w82), .a1(w80), .b(w109), .x(w110) );
	mmc1_aon g72 (.a0(w106), .a1(w79), .b(w82), .x(w157) );
	mmc1_oan g73 (.a0(w82), .a1(w80), .b(w106), .x(w112) );
	mmc1_33aon g74 (.a0(w107), .a1(w113), .a2(w77), .b0(w82), .b1(w79), .b2(w78), .x(w84) );
	mmc1_3or g75 (.a(w78), .b(w80), .c(w82), .x(w107) );
	mmc1_latch g76 (.d(w35), .cck(w117), .ck(w104), .q(w78) );
	mmc1_latch g77 (.d(w73), .cck(w117), .ck(w104), .q(w106) );
	mmc1_latch g78 (.d(w85), .cck(w117), .ck(w104), .q(w109) );
	mmc1_latch g79 (.d(w73), .cck(w83), .ck(w81), .q(w94) );
	mmc1_latch g80 (.d(w89), .cck(w83), .ck(w81), .q(w33) );
	mmc1_dff g81 (.d(w49), .cck(w76), .ck(w75), .q(w89) );
	mmc1_dff g82 (.d(w89), .cck(w76), .ck(w75), .q(w74) );
	mmc1_latch g83 (.d(w74), .cck(w117), .ck(w104), .q(w90) );
	mmc1_latch g84 (.d(w89), .cck(w117), .ck(w104), .q(w100) );
	mmc1_22aon g85 (.a0(w80), .a1(w90), .b0(w82), .b1(w90), .x(w103) );
	mmc1_33aon g86 (.a0(w101), .a1(w90), .a2(w77), .b0(w101), .b1(w82), .b2(w79), .x(w93) );
	mmc1_nand g87 (.a(w96), .b(w50), .x(w95) );
	mmc1_not g88 (.a(w94), .x(w98) );
	mmc1_222aon g89 (.a0(w103), .a1(w101), .b0(w93), .b1(w113), .c0(w90), .c1(w100), .x(w2) );
	mmc1_dffrs g90 (.nset1(w86), .d(w85), .cck(w83), .ck(w81), .nset2(w86), .nres(w77), .q(w82) );
	mmc1_dffrs g91 (.nset1(w86), .d(w74), .cck(w83), .ck(w81), .nset2(w86), .nres(w77), .q(w79) );
	mmc1_dff g92 (.d(w73), .cck(w76), .ck(w75), .q(w35) );
	mmc1_latch g93 (.d(w74), .cck(w88), .ck(w138), .q(w139) );
	mmc1_latch g94 (.d(w73), .cck(w88), .ck(w138), .q(w140) );
	mmc1_latch g95 (.d(w35), .cck(w83), .ck(w81), .q(w144) );
	mmc1_latch g96 (.d(w85), .cck(w88), .ck(w138), .q(w141) );
	mmc1_latch g97 (.d(w89), .cck(w88), .ck(w138), .q(w142) );
	mmc1_dff g98 (.d(w85), .cck(w76), .ck(w75), .q(w73) );
	mmc1_dff g99 (.d(w74), .cck(w76), .ck(w75), .q(w85) );
	mmc1_not g100 (.a(w79), .x(w80) );
	mmc1_latch g101 (.d(w73), .cck(w145), .ck(w99), .q(w146) );
	mmc1_latch g102 (.d(w89), .cck(w145), .ck(w99), .q(w148) );
	mmc1_not3 g103 (.a(w147), .x(w76) );
	mmc1_not g104 (.a(w144), .x(w91) );
	mmc1_333aon g105 (.a0(w62), .a1(w144), .a2(w94), .b0(w91), .b1(w94), .b2(w97), .c0(w77), .c1(w144), .c2(w98), .x(w92) );
	mmc1_buf g106 (.a(w3), .x(w7) );
	mmc1_buf g107 (.a(w4), .x(w3) );
	mmc1_buf g108 (.a(w5), .x(w4) );
	mmc1_buf g109 (.a(w7), .x(w8) );
	mmc1_buf g110 (.a(w9), .x(w5) );
	mmc1_buf g111 (.a(w11), .x(w9) );
	mmc1_buf g112 (.a(w8), .x(w13) );
	mmc1_buf g113 (.a(w87), .x(w12) );
	mmc1_buf g114 (.a(w12), .x(w10) );
	mmc1_buf g115 (.a(w25), .x(w11) );
	mmc1_buf g116 (.a(w10), .x(w26) );
	mmc1_buf g117 (.a(w13), .x(w137) );
	mmc1_buf g118 (.a(w115), .x(w138) );
	mmc1_not g119 (.a(w115), .x(w88) );
	mmc1_const g120 (.q1(w77) );
	mmc1_latch g121 (.d(w35), .cck(w88), .ck(w138), .q(w14) );
	mmc1_or g122 (.a(w14), .b(w31), .x(w32) );
	mmc1_or g123 (.a(w31), .b(w34), .x(w22) );
	mmc1_33aon g124 (.a0(w77), .a1(w32), .a2(w47), .b0(w34), .b1(w33), .b2(w30), .x(w27) );
	mmc1_not g125 (.a(w33), .x(w31) );
	mmc1_not2 g126 (.a(w47), .x(w34) );
	mmc1_and g127 (.a(w33), .b(w47), .x(w23) );
	mmc1_latch g128 (.d(w35), .cck(w145), .ck(w99), .q(w30) );
	mmc1_latch g129 (.d(w85), .cck(w145), .ck(w99), .q(w29) );
	mmc1_latch g130 (.d(w74), .cck(w145), .ck(w99), .q(w28) );
	mmc1_22aon g131 (.a0(w22), .a1(w29), .b0(w23), .b1(w141), .x(w18) );
	mmc1_22aon g132 (.a0(w22), .a1(w28), .b0(w23), .b1(w139), .x(w17) );
	mmc1_buf g133 (.a(w24), .x(w99) );
	mmc1_not g134 (.a(w24), .x(w145) );
	mmc1_22aon g135 (.a0(w22), .a1(w146), .b0(w140), .b1(w23), .x(w19) );
	mmc1_22aon g136 (.a0(w22), .a1(w148), .b0(w23), .b1(w142), .x(w143) );
	mmc1_not2 g137 (.a(w95), .x(w147) );
	mmc1_not3 g138 (.a(w95), .x(w75) );
	mmc1_not g139 (.a(w21), .x(w96) );
	mmc1_buf g140 (.a(w26), .x(w21) );
	mmc1_buf g141 (.a(w20), .x(w25) );
	mmc1_buf g142 (.a(w21), .x(w20) );
endmodule // mmc1

// Module Definitions [It is possible to wrap here on your primitives]

module mmc1_not (  a, x);

	input wire a;
	output wire x;

endmodule // mmc1_not

module mmc1_and (  a, b, x);

	input wire a;
	input wire b;
	output wire x;

endmodule // mmc1_and

module mmc1_or (  a, b, x);

	input wire a;
	input wire b;
	output wire x;

endmodule // mmc1_or

module mmc1_not2 (  a, x);

	input wire a;
	output wire x;

endmodule // mmc1_not2

module mmc1_buf2 (  a, x);

	input wire a;
	output wire x;

endmodule // mmc1_buf2

module mmc1_buf (  a, x);

	input wire a;
	output wire x;

endmodule // mmc1_buf

module mmc1_dff (  d, cck, ck, q);

	input wire d;
	input wire cck;
	input wire ck;
	output wire q;

endmodule // mmc1_dff

module mmc1_nand (  a, b, x);

	input wire a;
	input wire b;
	output wire x;

endmodule // mmc1_nand

module mmc1_nor (  a, b, x);

	input wire a;
	input wire b;
	output wire x;

endmodule // mmc1_nor

module mmc1_3or (  a, b, c, x);

	input wire a;
	input wire b;
	input wire c;
	output wire x;

endmodule // mmc1_3or

module mmc1_aon (  a0, a1, b, x);

	input wire a0;
	input wire a1;
	input wire b;
	output wire x;

endmodule // mmc1_aon

module mmc1_4and (  a, b, c, d, x);

	input wire a;
	input wire b;
	input wire c;
	input wire d;
	output wire x;

endmodule // mmc1_4and

module mmc1_dffr (  nres, d, cck, ck, q);

	input wire nres;
	input wire d;
	input wire cck;
	input wire ck;
	output wire q;

endmodule // mmc1_dffr

module mmc1_dffnq (  d, cck, ck, nq, q);

	input wire d;
	input wire cck;
	input wire ck;
	output wire nq;
	output wire q;

endmodule // mmc1_dffnq

module mmc1_dffrnq (  nres, d, cck, ck, q, nq);

	input wire nres;
	input wire d;
	input wire cck;
	input wire ck;
	output wire q;
	output wire nq;

endmodule // mmc1_dffrnq

module mmc1_3nand (  a, b, c, x);

	input wire a;
	input wire b;
	input wire c;
	output wire x;

endmodule // mmc1_3nand

module mmc1_oan (  a0, a1, b, x);

	input wire a0;
	input wire a1;
	input wire b;
	output wire x;

endmodule // mmc1_oan

module mmc1_33aon (  a0, a1, a2, b0, b1, b2, x);

	input wire a0;
	input wire a1;
	input wire a2;
	input wire b0;
	input wire b1;
	input wire b2;
	output wire x;

endmodule // mmc1_33aon

module mmc1_latch (  d, cck, ck, q);

	input wire d;
	input wire cck;
	input wire ck;
	output wire q;

endmodule // mmc1_latch

module mmc1_22aon (  a0, a1, b0, b1, x);

	input wire a0;
	input wire a1;
	input wire b0;
	input wire b1;
	output wire x;

endmodule // mmc1_22aon

module mmc1_222aon (  a0, a1, b0, b1, c0, c1, x);

	input wire a0;
	input wire a1;
	input wire b0;
	input wire b1;
	input wire c0;
	input wire c1;
	output wire x;

endmodule // mmc1_222aon

module mmc1_dffrs (  nset1, d, cck, ck, nset2, nres, q);

	input wire nset1;
	input wire d;
	input wire cck;
	input wire ck;
	input wire nset2;
	input wire nres;
	output wire q;

endmodule // mmc1_dffrs

module mmc1_not3 (  a, x);

	input wire a;
	output wire x;

endmodule // mmc1_not3

module mmc1_333aon (  a0, a1, a2, b0, b1, b2, c0, c1, c2, x);

	input wire a0;
	input wire a1;
	input wire a2;
	input wire b0;
	input wire b1;
	input wire b2;
	input wire c0;
	input wire c1;
	input wire c2;
	output wire x;

endmodule // mmc1_333aon

module mmc1_const (  q0, q1);

	output wire q0;
	output wire q1;

endmodule // mmc1_const



// WARNING: Cell mmc1_const:g120 port q0 not connected.
